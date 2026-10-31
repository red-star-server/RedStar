using System.Numerics;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Data;
using Content.Server._RedStar.Beekeeping.Events;
using Content.Server.Audio;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
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
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private WeldableSystem _weldable = default!;

    private const float DefaultShrinkTime = 0.6f;

    /// <summary>
    /// How close to the hive a bee flies before it goes in, in tiles. The hive is solid: the bee stops next to it.
    /// </summary>
    private const float HomeRange = 1f;

    /// <summary>
    /// How fast a bee goes through the pipes, in tiles per second of straight distance between the two vents.
    /// </summary>
    private const float PipeSpeed = 3f;

    /// <summary>
    /// How many times a bee flies on to a target that moved before it gives up and goes home.
    /// </summary>
    private const int MaxRetargets = 6;

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
    /// Where the bee flies next and how close it has to get. False when there is nowhere to go: the hive is gone or the
    /// bee has nothing left to do on the way out.
    /// </summary>
    public bool TryGetNextStop(EntityUid uid, out EntityCoordinates coordinates, out float range)
    {
        coordinates = default;
        range = 0f;
        if (!TryComp<BeeErrandComponent>(uid, out var errand) || errand.Act != BeeErrandAct.None)
            return false;

        range = errand.StopRange;
        EntityUid? stop;
        if (errand.Stage == BeeErrandStage.Out)
        {
            if (errand.VentEntry != null && !errand.PipedOut)
            {
                stop = OpenVent(errand.VentEntry);
                if (stop == null)
                    ClearRoute(errand);
            }
            else
            {
                stop = null;
            }

            if (stop == null && errand.Target is { } target && !TerminatingOrDeleted(target))
                stop = target;

            if (stop == null)
            {
                // A spot to fly to, or nothing: then the bee acts where it is, and whoever sent it says what next.
                coordinates = errand.Spot ?? Transform(uid).Coordinates;
                range = errand.Spot == null ? float.MaxValue : errand.StopRange;
                Face(uid, coordinates);
                return true;
            }
        }
        else
        {
            if (TerminatingOrDeleted(errand.Hive) || _container.IsEntityInContainer(errand.Hive) && _scout.StorageOf(errand.Hive) == null)
                return false;

            stop = errand.VentExit != null && errand.PipedOut && !errand.PipedBack ? OpenVent(errand.VentExit) : null;
            if (stop == null)
            {
                stop = errand.Hive;
                range = HomeRange;
            }
        }

        // A locker a wild hive sits in is flown to, not the hive inside it.
        coordinates = Transform(_scout.StorageOf(stop) ?? stop.Value).Coordinates;
        Face(uid, coordinates);
        return true;
    }

    /// <summary>
    /// The vent, if it is still there and open.
    /// </summary>
    private EntityUid? OpenVent(EntityUid? vent)
    {
        return vent is { } uid && !TerminatingOrDeleted(uid) && !_weldable.IsWelded(uid) ? uid : null;
    }

    private static void ClearRoute(BeeErrandComponent errand)
    {
        errand.VentEntry = null;
        errand.VentExit = null;
    }

    /// <summary>
    /// A new target or spot for a bee that is on its way out: a creature that moved, a swarm's next hop.
    /// </summary>
    public void SetTarget(Entity<BeeErrandComponent?> bee, EntityUid? target, EntityCoordinates? spot = null)
    {
        if (!Resolve(bee, ref bee.Comp))
            return;

        bee.Comp.Target = target;
        bee.Comp.Spot = spot;
    }

    /// <summary>
    /// A way through the pipes from <paramref name="entry"/> to <paramref name="exit"/> for a bee on its way out.
    /// </summary>
    public void SetRoute(Entity<BeeErrandComponent?> bee, EntityUid entry, EntityUid exit)
    {
        if (!Resolve(bee, ref bee.Comp))
            return;

        bee.Comp.VentEntry = entry;
        bee.Comp.VentExit = exit;
        bee.Comp.PipedOut = false;
    }

    /// <summary>
    /// The bee flies faster (or slower) than its prototype says: a furious meat colony's bees.
    /// </summary>
    public void SetSpeed(EntityUid bee, float multiplier)
    {
        if (!TryComp<MovementSpeedModifierComponent>(bee, out var move))
            return;

        _speed.ChangeBaseSpeed(bee,
            move.BaseWalkSpeed * multiplier,
            move.BaseSprintSpeed * multiplier,
            move.Acceleration,
            move);
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
            if (errand.VentEntry != null && !errand.PipedOut)
            {
                DiveInto(bee, BeeErrandAct.Pipe, errand.VentEntry.Value);
                return;
            }

            Arrive(bee);
            return;
        }

        if (errand.VentExit != null && errand.PipedOut && !errand.PipedBack)
        {
            DiveInto(bee, BeeErrandAct.Pipe, errand.VentExit.Value);
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
    /// At the target: the system of that kind of bee does what the errand is for; a forager or the scout visits the
    /// flower.
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

        var ev = new BeeErrandArrivedEvent(bee.Comp.Target);
        RaiseLocalEvent(bee, ref ev);

        // An empty spot with nothing handling it: there is nothing to do there, go home.
        var result = ev.Handled ? ev.Result
            : bee.Comp.Target == null ? BeeErrandArrival.TurnBack
            : BeeErrandArrival.Visit;

        switch (result)
        {
            case BeeErrandArrival.Visit:
                if (bee.Comp.Target is { } visited)
                    DiveInto(bee, BeeErrandAct.Visit, visited);
                else
                    StartStep(bee, BeeErrandAct.Visit, BeeFlightSize.Shrinking, ShrinkTime(bee));
                break;
            case BeeErrandArrival.TurnBack:
                bee.Comp.Stage = BeeErrandStage.Back;
                break;
            case BeeErrandArrival.KeepGoing:
                // A target that keeps moving off is given up after a few tries; a searching swarm keeps on.
                if (bee.Comp.Kind != BeeErrandKind.Swarm && ++bee.Comp.Retargets > MaxRetargets)
                    bee.Comp.Stage = BeeErrandStage.Back;
                break;
            case BeeErrandArrival.Gone:
                End(bee);
                break;
        }
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
            case BeeErrandAct.Pipe:
                return NextPipeStep(bee);
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
    /// Into the vent, through the pipes, and out of the vent at the other end. If that one is shut by now, the bee is
    /// lost in the pipes.
    /// </summary>
    private HTNOperatorStatus NextPipeStep(Entity<BeeErrandComponent> bee)
    {
        var comp = bee.Comp;
        var outward = !comp.PipedOut;
        switch (comp.ActStep)
        {
            case 0:
                StartStep(bee, BeeErrandAct.Pipe, BeeFlightSize.Hidden, PipeTime(comp), 1);
                return HTNOperatorStatus.Continuing;
            case 1:
                if (OpenVent(outward ? comp.VentExit : comp.VentEntry) is not { } exit)
                {
                    QueueDel(bee);
                    return HTNOperatorStatus.Failed;
                }

                _transform.SetCoordinates(bee, Transform(exit).Coordinates);
                Hide(bee, false);
                StartStep(bee, BeeErrandAct.Pipe, BeeFlightSize.Growing, ShrinkTime(bee), 2);
                return HTNOperatorStatus.Continuing;
            default:
                comp.Act = BeeErrandAct.None;
                comp.ActStep = 0;
                if (outward)
                    comp.PipedOut = true;
                else
                    comp.PipedBack = true;
                return HTNOperatorStatus.Finished;
        }
    }

    /// <summary>
    /// How long the bee is in the pipes: the straight distance between the two vents over the pipe speed, and a bit.
    /// </summary>
    private TimeSpan PipeTime(BeeErrandComponent errand)
    {
        if (errand.VentEntry is not { } entry || errand.VentExit is not { } exit ||
            TerminatingOrDeleted(entry) || TerminatingOrDeleted(exit))
        {
            return TimeSpan.FromSeconds(1);
        }

        var distance = (_transform.GetWorldPosition(entry) - _transform.GetWorldPosition(exit)).Length();
        return TimeSpan.FromSeconds(distance / PipeSpeed + 0.5f);
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

        var ev = new BeeErrandHomeEvent(bee.Comp.Hive);
        RaiseLocalEvent(bee, ref ev);
        End(bee);
    }

    /// <summary>
    /// The errand is over without the bee being lost: it is home, or it built a hive, or it scattered.
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
    /// Inside a flower or the pipes the bee is out of the way: nothing bumps into it or hits it.
    /// </summary>
    private void Hide(EntityUid bee, bool hidden)
    {
        _physics.SetCanCollide(bee, !hidden);
    }

    #endregion
}
