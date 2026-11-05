using System.Numerics;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Data;
using Content.Server.Audio;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// Bees out of the hive on errands (<see cref="BeeErrandComponent"/>). They are NPCs: the HTN task flies them with the
/// engine's pathfinding and steering, and this system only says where the next stop is and what the bee does there.
/// The colony's numbers do not depend on them: a visit's nectar is counted by the hive, the forager only acts it out.
/// A bee that is killed or never finds its way home is a bee the colony loses.
/// </summary>
public sealed partial class BeeErrandSystem : EntitySystem
{
    [Dependency] private AmbientSoundSystem _ambient = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private BeehiveSystem _hive = default!;
    [Dependency] private HTNSystem _htn = default!;
    [Dependency] private NPCSystem _npc = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private BeeQueenSystem _queen = default!;
    [Dependency] private BeeScoutSystem _scout = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private const float DefaultShrinkTime = 0.6f;

    /// <summary>
    /// How close to the hive a bee flies before it goes in, in tiles. The hive is solid: the bee stops next to it.
    /// </summary>
    private const float HomeRange = 1f;

    /// <summary>
    /// The HTN task of a bee on an errand.
    /// </summary>
    private static readonly ProtoId<HTNCompoundPrototype> ErrandTask = "BeeErrandCompound";

    /// <summary>
    /// How far from a hive a player has to be for its visible bees to be worth showing, in tiles: about a screen.
    /// </summary>
    private const float WatchRange = 15f;

    private readonly HashSet<Entity<BeeErrandComponent>> _errandsNear = new();
    private readonly HashSet<Entity<ActorComponent>> _watchers = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Only the bees diving into something right now, for the fraction of a second it takes.
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BeeErrandComponent>();
        while (query.MoveNext(out var uid, out var errand))
        {
            if (errand.GlideFrom is not { } from)
                continue;

            var total = (errand.GlideEnd - errand.GlideStart).TotalSeconds;
            var progress = total > 0 ? (float) Math.Clamp((now - errand.GlideStart).TotalSeconds / total, 0, 1) : 1f;
            _transform.SetWorldPosition(uid, Vector2.Lerp(from, errand.GlideTo, progress));
            if (progress >= 1f)
                errand.GlideFrom = null;
        }
    }

    #region The way

    /// <summary>
    /// Where the bee flies next and how close it has to get. False when there is nowhere to go: the hive is gone.
    /// </summary>
    public bool TryGetNextStop(EntityUid uid, out EntityCoordinates coordinates, out float range)
    {
        coordinates = default;
        range = 0f;
        if (!TryComp<BeeErrandComponent>(uid, out var errand) || errand.Act != BeeErrandAct.None)
            return false;

        range = errand.StopRange;
        EntityUid stop;
        if (errand.Stage == BeeErrandStage.Out)
        {
            if (errand.Target is not { } target || TerminatingOrDeleted(target))
            {
                // Nothing left to fly to (the tray is gone): the bee acts where it is.
                coordinates = Transform(uid).Coordinates;
                range = float.MaxValue;
                Face(uid, coordinates);
                return true;
            }

            stop = target;
        }
        else
        {
            if (TerminatingOrDeleted(errand.Hive) || _container.IsEntityInContainer(errand.Hive) && _scout.StorageOf(errand.Hive) == null)
                return false;

            stop = errand.Hive;
            range = HomeRange;
        }

        // A locker a wild hive sits in is flown to, not the hive inside it.
        coordinates = Transform(_scout.StorageOf(stop) ?? stop).Coordinates;
        Face(uid, coordinates);
        return true;
    }

    #endregion

    #region At a stop

    /// <summary>
    /// The bee reached its stop: what it does there.
    /// </summary>
    public void StartAct(EntityUid uid)
    {
        if (!TryComp<BeeErrandComponent>(uid, out var errand))
            return;

        // It stops where it is to act: nothing steers it until the next stop is planned, and a flying body keeps
        // drifting.
        _physics.SetLinearVelocity(uid, Vector2.Zero);

        var bee = new Entity<BeeErrandComponent>(uid, errand);
        if (errand.Stage == BeeErrandStage.Out)
        {
            Arrive(bee);
            return;
        }

        DiveInto(bee, BeeErrandAct.Enter, _scout.StorageOf(errand.Hive) ?? errand.Hive);
    }

    /// <summary>
    /// The bee stopped next to what it dives into (it cannot fly into a solid hive or tray), so it glides the last bit
    /// to the middle while it shrinks, with its body out of the way.
    /// </summary>
    private void DiveInto(Entity<BeeErrandComponent> bee, BeeErrandAct act, EntityUid into)
    {
        var length = ShrinkTime(bee);
        StartStep(bee, act, BeeFlightSize.Shrinking, length);
        Hide(bee, true);
        if (TerminatingOrDeleted(into))
            return;

        bee.Comp.GlideFrom = _transform.GetWorldPosition(bee);
        bee.Comp.GlideTo = _transform.GetWorldPosition(into);
        bee.Comp.GlideStart = _timing.CurTime;
        bee.Comp.GlideEnd = _timing.CurTime + length;
    }

    /// <summary>
    /// At the target: a forager or the scout visits the flower. With nothing to visit it turns back.
    /// </summary>
    private void Arrive(Entity<BeeErrandComponent> bee)
    {
        // Into fire or air its hive could not work in, a bee does not dive: it turns back.
        if (bee.Comp.Target is { } target &&
            !TerminatingOrDeleted(target) &&
            TryComp<BeehiveComponent>(bee.Comp.Hive, out var hive) &&
            _hive.AirFor(hive, target) != BeehivePauseReason.None)
        {
            bee.Comp.Stage = BeeErrandStage.Back;
            return;
        }

        if (bee.Comp.Target is { } visited)
            DiveInto(bee, BeeErrandAct.Visit, visited);
        else
            bee.Comp.Stage = BeeErrandStage.Back;
    }

    public HTNOperatorStatus UpdateAct(EntityUid uid)
    {
        if (!TryComp<BeeErrandComponent>(uid, out var errand) || TerminatingOrDeleted(uid))
            return HTNOperatorStatus.Failed;

        var bee = new Entity<BeeErrandComponent>(uid, errand);
        if (errand.Act == BeeErrandAct.None)
            return HTNOperatorStatus.Finished;

        if (_timing.CurTime < errand.ActStepEnd)
            return HTNOperatorStatus.Continuing;

        switch (errand.Act)
        {
            case BeeErrandAct.Visit:
                return NextVisitStep(bee);
            case BeeErrandAct.Enter:
                EnterHive(bee);
                break;
        }

        return HTNOperatorStatus.Finished;
    }

    /// <summary>
    /// Into the flower, a while inside, and back out: then home.
    /// </summary>
    private HTNOperatorStatus NextVisitStep(Entity<BeeErrandComponent> bee)
    {
        switch (bee.Comp.ActStep)
        {
            case 0:
                StartStep(bee, BeeErrandAct.Visit, BeeFlightSize.Hidden, bee.Comp.VisitTime, 1);
                return HTNOperatorStatus.Continuing;
            case 1:
                Hide(bee, false);
                StartStep(bee, BeeErrandAct.Visit, BeeFlightSize.Growing, ShrinkTime(bee), 2);
                return HTNOperatorStatus.Continuing;
            default:
                bee.Comp.Act = BeeErrandAct.None;
                bee.Comp.ActStep = 0;
                bee.Comp.Stage = BeeErrandStage.Back;
                return HTNOperatorStatus.Finished;
        }
    }

    /// <summary>
    /// The bee went into its hive: the errand is over. A scout's findings become the hive's.
    /// </summary>
    private void EnterHive(Entity<BeeErrandComponent> bee)
    {
        // A bee taken out of the hive comes back like one brought by hand; turned away, it stays outside.
        if (TryComp<BeeItemComponent>(bee, out var item) && item.CountedOutside)
        {
            bee.Comp.Ended = true;
            if (TryComp<BeehiveComponent>(bee.Comp.Hive, out var hive) &&
                _hive.TryReturnBee((bee.Comp.Hive, hive), (bee, item), null))
            {
                return;
            }

            Hide(bee, false);
            SetSize(bee, BeeFlightSize.Full);
            RemComp<BeeErrandComponent>(bee);
            return;
        }

        if (bee.Comp.Kind == BeeErrandKind.Scout)
            _scout.LandScout(bee);

        End(bee);
    }

    /// <summary>
    /// The errand is over without the bee being lost: it is home.
    /// </summary>
    private void End(Entity<BeeErrandComponent> bee)
    {
        bee.Comp.Ended = true;
        bee.Comp.Act = BeeErrandAct.None;
        if (TryComp<BeehiveComponent>(bee.Comp.Hive, out var hive))
            hive.Foragers.Remove(bee);

        QueueDel(bee);
    }

    private void StartStep(Entity<BeeErrandComponent> bee, BeeErrandAct act, BeeFlightSize size, TimeSpan length, int step = 0)
    {
        bee.Comp.Act = act;
        bee.Comp.ActStep = step;
        bee.Comp.ActStepEnd = _timing.CurTime + length;
        SetSize(bee, size);
    }

    /// <summary>
    /// Inside a flower or the hive the bee is out of the way: nothing bumps into it or hits it.
    /// </summary>
    private void Hide(EntityUid bee, bool hidden)
    {
        _physics.SetCanCollide(bee, !hidden);
    }

    #endregion
}
