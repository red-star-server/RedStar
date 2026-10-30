using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Data;
using Content.Server._RedStar.Beekeeping.Events;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Robust.Shared.Player;
using Robust.Shared.Map.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Interaction;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.DoAfter;
using Content.Shared.Destructible;
using Content.Shared.Database;
using Content.Shared.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Components;
using Content.Shared.Random.Helpers;
using System.Numerics;
using Content.Server.Weapons.Melee;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// A meat colony: a wild hive that grew where there were creatures and no plants and feeds on blood. What a swarm
/// lives on is chosen here; the workers fly to creatures (through the vents when there is no straight way) and bite: a
/// little damage and a little real blood that goes into the hive, less for each further bite on the same creature. The
/// colony rages when its bees are killed, and a broken meat hive gives bloody combs and meat. Its data is in
/// <see cref="MeatBeehiveComponent"/>.
/// </summary>
public sealed partial class MeatBeehiveSystem : EntitySystem
{
    [Dependency] private WeldableSystem _weldable = default!;
    [Dependency] private BeeQueenSystem _queen = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private BeeErrandSystem _errands = default!;
    [Dependency] private AngryBeehiveSystem _angry = default!;
    [Dependency] private BeeSwarmSystem _swarm = default!;
    [Dependency] private BeehiveSystem _hive = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private BeeScoutSystem _scout = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MeleeWeaponSystem _melee = default!;
    [Dependency] private EntityQuery<BloodstreamComponent> _bloodQuery;

    private readonly HashSet<Entity<MobStateComponent>> _creatures = new();
    private readonly HashSet<Entity<MobStateComponent>> _smallCreatures = new();
    private readonly HashSet<EntityUid> _hunted = new();
    private readonly List<EntityUid> _expiredAttackers = new();
    private readonly List<(EntityUid Mob, float Weight, (EntityUid Entry, EntityUid Exit)? Route)> _huntCandidates = new();
    private readonly Dictionary<int, float> _huntWeights = new();

    /// <summary>
    /// The data of a hive that lives on blood; a hive that has just turned to meat gets it here.
    /// </summary>
    public MeatBeehiveComponent MeatOf(EntityUid hive)
    {
        return EnsureComp<MeatBeehiveComponent>(hive);
    }

    /// <summary>
    /// The color a meat colony's bees show once blood has been brought: the mix of the kinds it has had. Null for a
    /// colony that has brought none, which keeps the honey color.
    /// </summary>
    public Color? MeatLook(Entity<BeehiveComponent> hive)
    {
        if (hive.Comp.Diet != BeehiveDiet.Meat)
            return null;

        var meatComp = MeatOf(hive);

        // Red from the start: the plain blood color until the colony has brought some of its own.
        if (meatComp.BloodTypes.Count == 0)
            return _proto.TryIndex(meatComp.DefaultBlood, out var plain) ? Vivid(plain.SubstanceColor) : null;

        float r = 0f, g = 0f, b = 0f;
        var count = 0;
        foreach (var blood in meatComp.BloodTypes)
        {
            if (!_proto.TryIndex(blood, out var proto))
                continue;

            r += proto.SubstanceColor.R;
            g += proto.SubstanceColor.G;
            b += proto.SubstanceColor.B;
            count++;
        }

        return count == 0 ? null : Vivid(new Color(r / count, g / count, b / count));
    }

    /// <summary>
    /// The blood color made bright enough to show on a bee: the grey mask of a bee is light, and the color of blood
    /// itself (dark red) makes it look grey-brown. The brightest channel goes to 0.9, the hue stays.
    /// </summary>
    private static Color Vivid(Color color)
    {
        var brightest = Math.Max(color.R, Math.Max(color.G, color.B));
        if (brightest <= 0.01f)
            return color;

        var scale = 0.9f / brightest;
        return new Color(Math.Min(1f, color.R * scale), Math.Min(1f, color.G * scale), Math.Min(1f, color.B * scale));
    }

    /// <summary>
    /// Whether the hive can use this creature as a source now: alive or dead, with blood left, on the hive's grid, not
    /// inside anything, in range and in a clear straight line. Small creatures count only when enough of them are
    /// around.
    /// </summary>
    public BeeSourceCheck CheckCreature(Entity<BeehiveComponent> hive,
        TransformComponent hiveXform,
        MapCoordinates hiveMap,
        EntityUid target)
    {
        var basic = CreatureReady(hiveXform, hiveMap, target, out var center);
        if (basic != BeeSourceCheck.Ripe)
            return basic;

        // In sight of the hive, or reachable through its vents.
        var direct = (center.Position - hiveMap.Position).Length() <= hive.Comp.VisitRange &&
                     _scout.ClearFlightPath(hiveMap, center, hive, target);
        if (!direct && FindHuntRoute(hive, hiveMap, target) == null)
            return BeeSourceCheck.Inactive;

        if (IsSmallCreature(hive.Comp, target) && !SmallGroupAround(hive, hiveMap))
            return BeeSourceCheck.Inactive;

        return BeeSourceCheck.Ripe;
    }

    /// <summary>
    /// What does not depend on the way to the creature: it exists and has blood, is on the hive's grid and map, and is not
    /// inside anything. Ripe here means only that.
    /// </summary>
    private BeeSourceCheck CreatureReady(TransformComponent hiveXform, MapCoordinates hiveMap, EntityUid target, out MapCoordinates center)
    {
        center = default;
        if (TerminatingOrDeleted(target) || !_bloodQuery.HasComp(target) || !HasComp<MobStateComponent>(target))
            return BeeSourceCheck.Invalid;

        var xform = Transform(target);
        if (hiveXform.GridUid == null ||
            xform.GridUid != hiveXform.GridUid ||
            _container.IsEntityInContainer(target))
        {
            return BeeSourceCheck.Inactive;
        }

        // A sealed suit with a helmet is no use to the bees: it is not a target.
        if (_angry.IsStingProof(target))
            return BeeSourceCheck.Inactive;

        center = _transform.GetMapCoordinates(target, xform);
        return center.MapId != hiveMap.MapId || !HasBlood(target) ? BeeSourceCheck.Inactive : BeeSourceCheck.Ripe;
    }

    private bool HasBlood(EntityUid target)
    {
        return _bloodQuery.TryComp(target, out var blood) &&
               _solutions.ResolveSolution(target, blood.BloodSolutionName, ref blood.BloodSolution, out var solution) &&
               solution.Volume > FixedPoint2.Zero;
    }

    private bool IsSmallCreature(BeehiveComponent comp, EntityUid target)
    {
        return TryComp<PhysicsComponent>(target, out var physics) && physics.FixturesMass < comp.SmallCreatureMass;
    }

    /// <summary>
    /// At least <see cref="BeehiveComponent.MinSmallCreatures"/> small creatures with blood in the hive's range.
    /// </summary>
    private bool SmallGroupAround(Entity<BeehiveComponent> hive, MapCoordinates hiveMap)
    {
        // Its own set: this runs while the callers are still going through theirs.
        _smallCreatures.Clear();
        _lookup.GetEntitiesInRange(hiveMap, hive.Comp.VisitRange, _smallCreatures);
        var count = 0;
        foreach (var mob in _smallCreatures)
        {
            if (IsSmallCreature(hive.Comp, mob) && _bloodQuery.HasComp(mob) && !_container.IsEntityInContainer(mob) &&
                HasBlood(mob) && ++count >= hive.Comp.MinSmallCreatures)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A meat hive has no scout: creatures move too much to be remembered. When a worker is free, the hive looks
    /// around once, picks a target in its range and sends a forager at it. Only those bitten lately are remembered,
    /// for the falloff of the bites, and forgotten when the window has passed.
    /// </summary>
    public void Hunt(Entity<BeehiveComponent> hive)
    {
        var meatComp = MeatOf(hive);
        var comp = hive.Comp;
        var now = _timing.CurTime;
        comp.KnownSources.RemoveAll(s => TerminatingOrDeleted(s.Tray) || now - s.LastSeen > meatComp.BiteFalloffWindow);
        _expiredAttackers.Clear();
        foreach (var (attacker, mark) in meatComp.Attackers)
        {
            if (now >= mark.Until)
                _expiredAttackers.Add(attacker);
        }

        foreach (var gone in _expiredAttackers)
        {
            meatComp.Attackers.Remove(gone);
        }

        var xform = Transform(hive);
        var hiveMap = _transform.GetMapCoordinates(hive, xform);
        if (PickHuntTarget(hive, xform, hiveMap, now, out var route) is not { } target)
            return;

        var rage = RageLevel(hive);
        if (_errands.TrySpawnForager(hive, xform, target, MaxMeatForagersNow(hive), meatComp.MeatForagerPrototype, route) is not { } bee)
            return;

        // A meat colony's bees take the color of the blood and, once it is furious, show angry eyes and fly faster.
        _queen.SetTraitVisuals(bee, MeatLook(hive), rage > 0f);
        if (rage > 0f)
            _errands.SetSpeed(bee, 1f + meatComp.RageSpeedBonus * rage);

        if (rage > 0f)
            UseRevenge(hive, target);

        if (_scout.AddSource(hive, target, now) is { } source)
        {
            source.Active = true;
            source.LastSeen = now;
            source.NextVisit = now + meatComp.BiteInterval;
        }

        comp.VisitBudget -= 1f;
    }

    /// <summary>
    /// Foragers of the hive that are really out: the list may still hold ones that are gone.
    /// </summary>
    private int ForagersOut(BeehiveComponent comp)
    {
        var count = 0;
        foreach (var forager in comp.Foragers)
        {
            if (!TerminatingOrDeleted(forager))
                count++;
        }

        return count;
    }

    /// <summary>
    /// How many foragers a meat colony may have out at once: more when it is furious.
    /// </summary>
    private int MaxMeatForagersNow(Entity<BeehiveComponent> hive)
    {
        var meatComp = MeatOf(hive);
        return meatComp.MaxMeatForagers + (int) MathF.Round(meatComp.RageExtraForagers * RageLevel(hive));
    }

    /// <summary>
    /// A creature the hive can bite now, by chance: the less it was bitten lately, the likelier, and a furious colony
    /// is likelier to go after whoever killed its bees. Creatures in sight of the hive and those near a vent of its
    /// network count; for the second kind the route through the pipes comes with the answer. One that another forager
    /// of the hive is already after, or was bitten a moment ago, is left alone.
    /// </summary>
    public EntityUid? PickHuntTarget(Entity<BeehiveComponent> hive,
        TransformComponent xform,
        MapCoordinates hiveMap,
        TimeSpan now,
        out (EntityUid Entry, EntityUid Exit)? route)
    {
        var meatComp = MeatOf(hive);
        route = null;
        var comp = hive.Comp;
        if (ForagersOut(comp) >= MaxMeatForagersNow(hive))
            return null;

        _hunted.Clear();
        foreach (var forager in comp.Foragers)
        {
            if (!TerminatingOrDeleted(forager) && TryComp<BeeErrandComponent>(forager, out var errand) && errand.Target is { } prey)
                _hunted.Add(prey);
        }

        // Everyone in range, and everyone near a vent of the network: each creature with the vents it could be reached
        // from, gathered once for the whole choice.
        _creatures.Clear();
        _lookup.GetEntitiesInRange(hiveMap, comp.VisitRange, _creatures);
        GatherCreaturesNearVents(hive, hiveMap);

        var rage = RageLevel(hive);
        _huntCandidates.Clear();
        var total = 0f;
        bool? smallGroup = null;
        foreach (var mob in _creatures)
        {
            if (_hunted.Contains(mob.Owner))
                continue;

            var weight = 1f;
            foreach (var source in comp.KnownSources)
            {
                if (source.Tray != mob.Owner)
                    continue;

                if (now < source.NextVisit)
                    weight = 0f;
                else if (now - source.BiteWindowStart <= meatComp.BiteFalloffWindow)
                    weight = BiteFactor(source.Bites, meatComp.BiteMinFactor);
                break;
            }

            weight *= AttackerBias(hive, mob.Owner, rage, now);
            if (weight <= 0f || CreatureReady(xform, hiveMap, mob, out var center) != BeeSourceCheck.Ripe)
                continue;

            // Straight in sight, or from one of the vents it was found near.
            (EntityUid Entry, EntityUid Exit)? way = null;
            var direct = (center.Position - hiveMap.Position).Length() <= comp.VisitRange &&
                         _scout.ClearFlightPath(hiveMap, center, hive, mob);
            if (!direct)
            {
                way = ClearVentRoute(mob, center);
                if (way == null)
                    continue;
            }

            if (IsSmallCreature(comp, mob))
            {
                smallGroup ??= SmallGroupAround(hive, hiveMap);
                if (!smallGroup.Value)
                    continue;
            }

            _huntCandidates.Add((mob.Owner, weight, way));
            total += weight;
        }

        if (total <= 0f)
            return null;

        _huntWeights.Clear();
        for (var i = 0; i < _huntCandidates.Count; i++)
        {
            _huntWeights[i] = _huntCandidates[i].Weight;
        }

        var chosen = _huntCandidates[_random.Pick(_huntWeights)];

        route = chosen.Route;
        return chosen.Mob;
    }

    /// <summary>
    /// How near a bee must be to a creature to bite it, in tiles.
    /// </summary>
    private const float BiteReach = 1.3f;

    /// <summary>
    /// A meat colony's bee reached its creature. Gone, or out of the hive's reach now: back home empty. Moved off: after
    /// it. Within reach: a bite, and home; on a corpse it lands for a while like on a flower.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnArrived(Entity<BeeHunterComponent> ent, ref BeeErrandArrivedEvent args)
    {
        args.Handled = true;
        args.Result = BeeErrandArrival.TurnBack;
        if (!TryComp<BeeErrandComponent>(ent, out var errand) ||
            args.Target is not { } creature ||
            TerminatingOrDeleted(errand.Hive) ||
            !TryComp<BeehiveComponent>(errand.Hive, out var comp))
        {
            return;
        }

        var hive = new Entity<BeehiveComponent>(errand.Hive, comp);
        var hiveXform = Transform(hive);
        if (CheckCreature(hive, hiveXform, _transform.GetMapCoordinates(hive, hiveXform), creature) != BeeSourceCheck.Ripe)
            return;

        var beeMap = _transform.GetMapCoordinates(ent);
        var creatureMap = _transform.GetMapCoordinates(creature);
        if (beeMap.MapId != creatureMap.MapId || (beeMap.Position - creatureMap.Position).Length() > BiteReach)
        {
            args.Result = BeeErrandArrival.KeepGoing;
            return;
        }

        if (BiteCreature(ent, hive, creature))
            args.Result = BeeErrandArrival.Visit;
    }

    /// <summary>
    /// How much of a full bite the next one on this creature is worth: each bite within the window halves it, down to
    /// the minimum. A new window starts when the old one has passed.
    /// </summary>
    public static float BiteFactor(int bites, float minimum)
    {
        return Math.Max(minimum, MathF.Pow(0.5f, Math.Max(bites, 0)));
    }

    /// <summary>
    /// The forager reached its creature: it bites and flies off. Blood, a little damage, nectar by the bite's worth.
    /// </summary>
    public bool BiteCreature(EntityUid bee, Entity<BeehiveComponent> hive, EntityUid target)
    {
        var meatComp = MeatOf(hive);
        var comp = hive.Comp;
        var now = _timing.CurTime;

        var factor = 1f;
        foreach (var source in comp.KnownSources)
        {
            if (source.Tray != target)
                continue;

            if (source.Bites == 0 || now - source.BiteWindowStart > meatComp.BiteFalloffWindow)
            {
                source.Bites = 0;
                source.BiteWindowStart = now;
            }

            source.LastSeen = now;

            factor = BiteFactor(source.Bites, meatComp.BiteMinFactor);
            source.Bites++;
            break;
        }

        var dead = _mobState.IsDead(target);

        // Suited up after the bee set off: it does not even swing, and the bee leaves without landing.
        if (_angry.IsStingProof(target))
        {
            _popup.PopupEntity(Loc.GetString("beekeeping-bite-blocked"), target, target, PopupType.Small);
            return false;
        }

        // The bite shows even when it finds no blood to take.
        if (!dead)
            ShowBite(bee, target);

        var taken = TakeBlood(hive, target, factor);
        if (taken <= FixedPoint2.Zero)
            return dead;

        _hive.AddNectar(hive, comp.NectarPerVisit * _hive.GetHiveTotals(hive).NectarPerVisit * factor);

        if (dead)
        {
            meatComp.Meat += meatComp.MeatPerDeadBite;
        }
        else
        {
            _damageable.TryChangeDamage(target, meatComp.MeatBiteDamage, origin: hive.Owner);
        }

        _audio.PlayPvs(meatComp.MeatBiteSound, target);
        return dead;
    }

    /// <summary>
    /// The bee lunges at the creature with the bite effect, the way the angry bees do. A living creature is bitten in
    /// passing; the bee does not land on it.
    /// </summary>
    private void ShowBite(EntityUid bee, EntityUid target)
    {
        var beeXform = Transform(bee);
        var targetPos = _transform.GetWorldPosition(target);
        var local = Vector2.Transform(targetPos, _transform.GetInvWorldMatrix(beeXform));
        if (local.LengthSquared() <= 0.0001f)
            return;

        local = beeXform.LocalRotation.RotateVec(local);
        if (local.Length() > 0.8f)
            local = local.Normalized() * 0.8f;

        _melee.DoLunge(bee, bee, Angle.Zero, local, "WeaponArcBite", predicted: false);
    }

    /// <summary>
    /// Takes a portion of the creature's blood into the hive's stock and remembers the kinds. Returns how much was
    /// taken.
    /// </summary>
    public FixedPoint2 TakeBlood(Entity<BeehiveComponent> hive, EntityUid target, float factor)
    {
        var meatComp = MeatOf(hive);
        var comp = hive.Comp;
        if (!_bloodQuery.TryComp(target, out var blood) ||
            !_solutions.ResolveSolution(target, blood.BloodSolutionName, ref blood.BloodSolution, out var bloodSolution) ||
            blood.BloodSolution is not { } solution)
        {
            return FixedPoint2.Zero;
        }

        var amount = FixedPoint2.Min(FixedPoint2.New(meatComp.BloodPerBite * factor), bloodSolution.Volume);
        if (amount <= FixedPoint2.Zero)
            return FixedPoint2.Zero;

        var taken = _solutions.SplitSolution(solution, amount);

        // Only the kinds of blood count as kinds: whatever else is in the blood (medicine, nutrients) neither colors the
        // bees nor makes the colony's blood more varied.
        var kinds = new MetabolismExclusionEvent();
        RaiseLocalEvent(target, ref kinds);
        foreach (var (reagent, _) in taken.Contents)
        {
            if (kinds.Reagents.Contains(reagent) && !meatComp.BloodTypes.Contains(reagent.Prototype))
                meatComp.BloodTypes.Add(reagent.Prototype);
        }

        meatComp.Blood.AddSolution(taken, _proto);
        var overflow = meatComp.Blood.Volume - FixedPoint2.New(meatComp.BloodStockCap);
        if (overflow > FixedPoint2.Zero)
            meatComp.Blood.SplitSolution(overflow);

        return taken.Volume;
    }
}
