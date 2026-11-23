using Content.Server.Atmos.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Stack;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Stacks;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared._RedStar.Beekeeping;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Prototypes;
using Content.Shared._RedStar.Beekeeping.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// The angry hive. A hit lets bees out: one per hit and one more for every <see cref="BeehiveComponent.AngryDamageStep"/>
/// of damage. Taking a frame out of a hive with aggressive bees lets some out too. The scout comes out first if it is
/// home. Angry bees come from the colony and fly back when their time is up; killed ones are lost.
/// When the hive breaks, the whole colony flies out and the queen defends it.
/// Limits: angry bees per hive and per map, hives with a queen per map.
/// </summary>
public sealed partial class AngryBeehiveSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MeatBeehiveSystem _meat = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private BeeQueenSystem _queen = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private BeeScoutSystem _scout = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private BeehiveSystem _hive = default!;
    [Dependency] private Content.Shared.StatusEffect.StatusEffectsSystem _status = default!;
    [Dependency] private BeeErrandSystem _errands = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private NPCSystem _npc = default!;

    private readonly HashSet<Entity<MobStateComponent>> _nearbyMobs = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateAngryQueens();
        UpdateReturningBees();
        UpdateCalmedBees();
        UpdateProofTargets();
    }

    private TimeSpan _nextProofCheck;
    private const float ProofCheckRange = 4f;

    #region Letting bees out

    [SubscribeLocalEvent]
    private void OnHiveDamaged(Entity<BeehiveComponent> ent, ref DamageDealtEvent args)
    {
        var damage = args.Damage.GetTotal().Float();
        if (damage <= 0f)
            return;

        var comp = ent.Comp;
        var traits = _hive.GetHiveTotals(ent);
        var bees = comp.AngryPerHit + traits.AngryPerHit;
        var step = comp.AngryDamageStep * traits.AngryDamageStep;
        if (step > 0f)
        {
            comp.AngryDamageProgress += damage;
            var steps = (int) (comp.AngryDamageProgress / step);
            comp.AngryDamageProgress -= steps * step;
            bees += steps;
        }

        var released = ReleaseAngryBees(ent, bees);
        if (released > 0)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(ent):hive} let out {released} angry bees, hit by {ToPrettyString(args.Origin):origin}");
        }
    }

    /// <summary>
    /// Someone hurt one of the hive's foragers or its scout: a bee or two come out after them.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnErrandBeeAttacked(ref BeeErrandAttackedEvent args)
    {
        if (!TryComp<BeehiveComponent>(args.Hive, out var comp) || _timing.CurTime < comp.NextDefense)
            return;

        comp.NextDefense = _timing.CurTime + comp.DefenseCooldown;
        ReleaseAngryBees((args.Hive, comp), DefendingBees, args.Attacker);
    }

    /// <summary>
    /// Angry bees one attack on a forager brings out.
    /// </summary>
    private const int DefendingBees = 2;

    /// <summary>
    /// A frame taken out of a hive with aggressive bees lets a few out.
    /// </summary>
    public void AngerOnFrameRemoved(Entity<BeehiveComponent> hive)
    {
        if (!_hive.GetHiveTotals(hive).Aggressive)
            return;

        var released = ReleaseAngryBees(hive, hive.Comp.FrameAngerBees);
        if (released > 0)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(hive):hive} let out {released} angry bees when a frame was taken out");
        }
    }

    /// <summary>
    /// Lets up to <paramref name="count"/> bees out of the hive as angry bees, the scout first if it is home.
    /// Stops at the hive's and the map's limits and when no bees are home. Returns how many flew out.
    /// </summary>
    private int ReleaseAngryBees(Entity<BeehiveComponent> hive, int count, EntityUid? after = null)
    {
        var comp = hive.Comp;

        // In water vapor the colony keeps calm.
        if (_hive.InCalmingVapor(hive, comp))
            return 0;

        var xform = Transform(hive);
        var mapLimit = _cfg.GetCVar(BeekeepingCVars.AngryBeeLimit);
        var onMap = CountAngryBeesOnMap(xform.MapUid);
        var released = 0;
        var guarded = _hive.GetHiveTotals(hive).GuardedScout;

        for (var i = 0; i < count; i++)
        {
            if (comp.Bees <= 0 || comp.AngryOut >= comp.MaxAngryBees || onMap >= mapLimit)
                break;

            // A guarded scout stays behind until the workers are out.
            var scout = comp.ScoutState == BeeScoutState.Home && SharedBeehiveSystem.ScoutsAtHome(comp) > 0 &&
                        (comp.AngryScout == null || TerminatingOrDeleted(comp.AngryScout.Value)) &&
                        (!guarded || SharedBeehiveSystem.WorkersAtHome(comp) == 0);
            var bee = Spawn(scout ? comp.AngryScoutPrototype : comp.AngryBeePrototype, xform.Coordinates);
            if (!TryComp<BeehiveAngryBeeComponent>(bee, out var angry))
            {
                Del(bee);
                break;
            }

            angry.ReturnAt = _timing.CurTime + (SwarmModifierOf(comp) is { AngryLifetime: > 0f } fierce
                ? TimeSpan.FromSeconds(fierce.AngryLifetime)
                : angry.Lifetime);

            angry.Hive = hive;
            angry.Scout = scout;
            angry.Counted = true;
            comp.Bees--;
            comp.OutBees++;
            comp.AngryOut++;

            if (scout)
            {
                comp.ScoutState = BeeScoutState.Outside;
                comp.AngryScout = bee;
                _scout.RecallScout(comp);
            }

            GiveAngryPayload(hive, (bee, angry));
            if (after is { } attacker)
                _faction.AggroEntity(bee, attacker);
            released++;
            onMap++;
        }

        if (released > 0)
        {
            _hive.UpdateAmbience(hive);
            _queen.Reveal(hive, BeeTraitReveal.Anger);
        }

        return released;
    }

    private SwarmModifierPrototype? SwarmModifierOf(BeehiveComponent comp)
    {
        return comp.SwarmModifier is { } id && _proto.TryIndex(id, out var modifier) ? modifier : null;
    }

    private int CountAngryBeesOnMap(EntityUid? map)
    {
        var count = 0;
        var query = EntityQueryEnumerator<BeehiveAngryBeeComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.MapUid == map)
                count++;
        }

        return count;
    }

    /// <summary>
    /// An angry bee from a hive with a sting takes some of the queen's payload and shows its colour.
    /// </summary>
    private void GiveAngryPayload(Entity<BeehiveComponent> hive, Entity<BeehiveAngryBeeComponent> bee)
    {
        Color? color = null;
        if (_hive.GetQueen(hive) is { } queenUid &&
            TryComp<BeeQueenComponent>(queenUid, out var queen) &&
            _hive.GetTotals(queen.Traits).Sting &&
            _solutions.TryGetSolution(queenUid, queen.Solution, out var soln, out var solution) &&
            solution.Volume > 0)
        {
            var taken = _solutions.SplitSolution(soln.Value,
                FixedPoint2.Min(FixedPoint2.New(hive.Comp.PayloadPerAngryBee), solution.Volume));
            bee.Comp.Payload.AddSolution(taken, _proto);
            color = taken.GetColor(_proto);
            _queen.UpdateQueenVisuals((queenUid, queen));
        }

        _queen.SetTraitVisuals(bee, color, true);
    }

    #endregion

    #region Angry bees

    /// <summary>
    /// Time is up: the bee flies back into its hive like any bee coming home, if the hive is still there. One that does
    /// not make it in time is lost.
    /// </summary>
    private void UpdateReturningBees()
    {
        var now = _timing.CurTime;
        var bees = EntityQueryEnumerator<BeehiveAngryBeeComponent>();
        while (bees.MoveNext(out var uid, out var angry))
        {
            if (angry.GiveUpAt is { } giveUp)
            {
                if (now >= giveUp)
                    QueueDel(uid);
                continue;
            }

            if (now < angry.ReturnAt)
                continue;

            angry.GiveUpAt = now + angry.ReturnTime;
            if (angry.Hive is { } hive && !TerminatingOrDeleted(hive))
                _errands.SendHome(uid, hive);
            else
                QueueDel(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnAngryBeeMapInit(Entity<BeehiveAngryBeeComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ReturnAt = _timing.CurTime + ent.Comp.Lifetime;
    }

    /// <summary>
    /// The bee made it into its hive: the colony has it back.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAngryBeeHome(Entity<BeehiveAngryBeeComponent> ent, ref BeeErrandHomeEvent args)
    {
        EndAngryBee(ent, true);
    }

    [SubscribeLocalEvent]
    private void OnAngryBeeState(Entity<BeehiveAngryBeeComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        EndAngryBee(ent, false);
        QueueDel(ent);
    }

    [SubscribeLocalEvent]
    private void OnAngryBeeTerminating(Entity<BeehiveAngryBeeComponent> ent, ref EntityTerminatingEvent args)
    {
        EndAngryBee(ent, false);
    }

    /// <summary>
    /// Closes the bee's count in its hive once: back home alive, or lost.
    /// </summary>
    private void EndAngryBee(Entity<BeehiveAngryBeeComponent> ent, bool home)
    {
        if (!ent.Comp.Counted)
            return;

        ent.Comp.Counted = false;
        if (ent.Comp.Hive is not { } hiveUid ||
            TerminatingOrDeleted(hiveUid) ||
            !TryComp<BeehiveComponent>(hiveUid, out var comp))
        {
            return;
        }

        var hive = new Entity<BeehiveComponent>(hiveUid, comp);
        _hive.ReleaseOutBee(hive);
        comp.AngryOut = Math.Max(0, comp.AngryOut - 1);
        if (home)
            comp.Bees++;

        if (ent.Comp.Scout)
        {
            comp.AngryScout = null;
            if (home)
            {
                comp.ScoutState = BeeScoutState.Home;
            }
            else
            {
                comp.ScoutState = BeeScoutState.Lost;
                comp.ScoutReplaceAt = _timing.CurTime + comp.ScoutReplaceDelay;
            }
        }

        _hive.UpdateAmbience(hive);
    }

    /// <summary>
    /// A sealed suit stops the sting: no damage and no payload. Otherwise the payload goes into the blood.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAngryBeeHit(Entity<BeehiveAngryBeeComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        foreach (var target in args.HitEntities)
        {
            if (IsStingProof(target))
            {
                args.BonusDamage = -args.BaseDamage;
                continue;
            }

            // A sting that got through shows whether the colony has one.
            if (ent.Comp.Hive is { } hive && TryComp<BeehiveComponent>(hive, out var hiveComp))
                _queen.Reveal((hive, hiveComp), BeeTraitReveal.Sting);

            if (ent.Comp.Payload.Volume <= 0)
                continue;

            var dose = ent.Comp.Payload.SplitSolution(FixedPoint2.Min(ent.Comp.PayloadPerSting, ent.Comp.Payload.Volume));
            _bloodstream.TryAddToBloodstream(target, dose);
        }
    }

    /// <summary>
    /// Outer clothing that holds pressure and a helmet that does too: a sealed suit.
    /// </summary>
    public bool IsStingProof(EntityUid target)
    {
        return _inventory.TryGetSlotEntity(target, "outerClothing", out var suit) &&
               HasComp<PressureProtectionComponent>(suit) &&
               _inventory.TryGetSlotEntity(target, "head", out var helmet) &&
               HasComp<PressureProtectionComponent>(helmet);
    }

    #endregion

    #region Colony limit

    #endregion

    #region Destruction

    /// <summary>
    /// A broken hive lets its whole colony out angry, and its queen defends it.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBeehiveBroken(ref BeehiveBrokenEvent args)
    {
        if (!TryComp<BeehiveComponent>(args.Hive, out var comp))
            return;

        var ent = new Entity<BeehiveComponent>(args.Hive, comp);
        var released = ReleaseAngryBees(ent, comp.Bees);
        if (_hive.GetQueen(ent) is { } queen)
            SpawnAngryQueen(ent, queen);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(ent):hive} was destroyed and let out {released} angry bees");
    }

    /// <summary>
    /// A hive taken apart with bees still inside: they all fly out angry. A destroyed hive has let them out already.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnHiveTerminating(Entity<BeehiveComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Bees <= 0)
            return;

        // The whole grid or map is going away (a round restart, a deleted shuttle): nowhere to fly to.
        var xform = Transform(ent);
        if (TerminatingOrDeleted(xform.ParentUid) || xform.MapUid is not { } map || TerminatingOrDeleted(map))
            return;

        var released = ReleaseAngryBees(ent, ent.Comp.Bees);
        if (released > 0)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(ent):hive} was taken apart and let out {released} angry bees");
        }
    }

    private void SpawnAngryQueen(Entity<BeehiveComponent> hive, EntityUid queen)
    {
        var xform = Transform(hive);
        var mob = Spawn(hive.Comp.AngryQueenPrototype, xform.Coordinates);
        if (!TryComp<BeehiveAngryQueenComponent>(mob, out var angry))
        {
            Del(mob);
            return;
        }

        var slot = _container.EnsureContainer<ContainerSlot>(mob, angry.Container);
        if (!_container.Insert(queen, slot, force: true))
        {
            Del(mob);
            return;
        }

        if (TryComp<BeeQueenComponent>(queen, out var queenComp))
        {
            var (color, _) = _queen.QueenLook((queen, queenComp));
            _queen.SetTraitVisuals(mob, color, true);
        }

        // The colony's own angry bees leave their queen alone.
        var bees = EntityQueryEnumerator<BeehiveAngryBeeComponent>();
        while (bees.MoveNext(out var bee, out var beeComp))
        {
            if (beeComp.Hive != hive.Owner)
                continue;

            var exceptions = EnsureComp<FactionExceptionComponent>(bee);
            _faction.IgnoreEntity((bee, exceptions), mob);
        }

        var now = _timing.CurTime;
        angry.AngrySince = now;
        angry.LastSeenTarget = now;
        angry.NextCheck = now + TimeSpan.FromSeconds(1);

        _nearbyMobs.Clear();
        _lookup.GetEntitiesInRange(xform.Coordinates, angry.TargetRange, _nearbyMobs);
        foreach (var target in _nearbyMobs)
        {
            if (target.Owner == mob ||
                !_mobState.IsAlive(target, target.Comp) ||
                IsStingProof(target) ||
                HasComp<BeehiveAngryBeeComponent>(target) ||
                HasComp<BeehiveAngryQueenComponent>(target))
            {
                continue;
            }

            _faction.AggroEntity(mob, target.Owner);
            angry.HadTargets = true;
        }
    }

    /// <summary>
    /// A person in a sealed suit is left alone by angry bees: they do not swing at them at all. The faction system
    /// skips the ones a bee ignores, so whoever is sealed near a bee is added to its ignore list, and whoever took the
    /// suit off is taken out of it again.
    /// </summary>
    private void UpdateProofTargets()
    {
        var now = _timing.CurTime;
        if (now < _nextProofCheck)
            return;

        _nextProofCheck = now + TimeSpan.FromSeconds(0.5);

        var bees = EntityQueryEnumerator<BeehiveAngryBeeComponent, FactionExceptionComponent, TransformComponent>();
        while (bees.MoveNext(out var uid, out _, out var exceptions, out var xform))
        {
            SweepProofTargets((uid, exceptions), xform);
        }

        var queens = EntityQueryEnumerator<BeehiveAngryQueenComponent, FactionExceptionComponent, TransformComponent>();
        while (queens.MoveNext(out var uid, out _, out var exceptions, out var xform))
        {
            SweepProofTargets((uid, exceptions), xform);
        }
    }

    private void SweepProofTargets(Entity<FactionExceptionComponent> bee, TransformComponent xform)
    {
        _nearbyMobs.Clear();
        _lookup.GetEntitiesInRange(xform.Coordinates, ProofCheckRange, _nearbyMobs);
        foreach (var target in _nearbyMobs)
        {
            if (target.Owner == bee.Owner || HasComp<BeehiveAngryBeeComponent>(target) || HasComp<BeehiveAngryQueenComponent>(target))
                continue;

            var proof = IsStingProof(target);
            var ignored = _faction.IsIgnored(bee.AsNullable(), target);
            if (proof && !ignored)
            {
                _faction.DeAggroEntity(bee.AsNullable(), target);
                _faction.IgnoreEntity(bee.AsNullable(), target.Owner);
            }
            else if (!proof && ignored)
            {
                _faction.UnignoreEntity(bee.AsNullable(), target);
            }
        }
    }

    /// <summary>
    /// Water or foam calms an angry bee or queen: while pacified its AI sleeps, so it stops going for anyone and
    /// only buzzes about where it is; then it wakes up angry again.
    /// </summary>
    private void UpdateCalmedBees()
    {
        // Water vapor calms angry bees and queens the way a splash of water does, for as long as they are in it.
        if (_timing.CurTime >= _nextVaporCheck)
        {
            _nextVaporCheck = _timing.CurTime + VaporCheckInterval;
            var vapor = EntityQueryEnumerator<BeehiveAngryBeeComponent>();
            while (vapor.MoveNext(out var uid, out _))
                CalmInVapor(uid);

            var vaporQueens = EntityQueryEnumerator<BeehiveAngryQueenComponent>();
            while (vaporQueens.MoveNext(out var uid, out _))
                CalmInVapor(uid);
        }

        var bees = EntityQueryEnumerator<BeehiveAngryBeeComponent, HTNComponent>();
        while (bees.MoveNext(out var uid, out var bee, out var htn))
        {
            bee.Calmed = UpdateCalm((uid, htn), bee.Calmed);
        }

        var queens = EntityQueryEnumerator<BeehiveAngryQueenComponent, HTNComponent>();
        while (queens.MoveNext(out var uid, out var queen, out var htn))
        {
            queen.Calmed = UpdateCalm((uid, htn), queen.Calmed);
        }
    }

    private static readonly TimeSpan VaporCheckInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextVaporCheck;

    private void CalmInVapor(EntityUid bee)
    {
        if (_hive.InCalmingVapor(bee))
            _status.TryAddStatusEffect<PacifiedComponent>(bee, "Pacified", VaporCheckInterval * 2, true);
    }

    private bool UpdateCalm(Entity<HTNComponent> mob, bool calmed)
    {
        var calm = HasComp<PacifiedComponent>(mob);
        if (calm == calmed)
            return calmed;

        if (calm)
            _npc.SleepNPC(mob, mob.Comp);
        else
            _npc.WakeNPC(mob, mob.Comp);
        return calm;
    }

    /// <summary>
    /// Once a second: does the queen still see a living target? If not for long enough, she lands.
    /// </summary>
    private void UpdateAngryQueens()
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BeehiveAngryQueenComponent, FactionExceptionComponent>();
        while (query.MoveNext(out var uid, out var queen, out var exceptions))
        {
            if (now < queen.NextCheck)
                continue;

            queen.NextCheck = now + TimeSpan.FromSeconds(1);

            var queenMap = _transform.GetMapCoordinates(uid);
            foreach (var target in exceptions.Hostiles)
            {
                if (TerminatingOrDeleted(target) || !_mobState.IsAlive(target))
                    continue;

                queen.HadTargets = true;
                if (_examine.InRangeUnOccluded(queenMap, _transform.GetMapCoordinates(target), queen.SightRange, null))
                {
                    queen.LastSeenTarget = now;
                    break;
                }
            }

            var calm = now - queen.AngrySince >= queen.MaxAngry ||
                       now - queen.LastSeenTarget >= (queen.HadTargets ? queen.CalmAfterLost : queen.CalmWithoutTargets);
            if (calm)
                LandAngryQueen((uid, queen));
        }
    }

    /// <summary>
    /// The queen calms down: the item is dropped where she is, with her traits and payload, and the mob is gone.
    /// </summary>
    private void LandAngryQueen(Entity<BeehiveAngryQueenComponent> queen)
    {
        if (_container.TryGetContainer(queen, queen.Comp.Container, out var container))
        {
            foreach (var item in new List<EntityUid>(container.ContainedEntities))
            {
                _container.Remove(item, container);
                _transform.DropNextTo(item, queen.Owner);
            }
        }

        QueueDel(queen);
    }

    [SubscribeLocalEvent]
    private void OnAngryQueenState(Entity<BeehiveAngryQueenComponent> ent, ref MobStateChangedEvent args)
    {
        // Killed, she is lost together with the item inside her.
        if (args.NewMobState == MobState.Dead)
            QueueDel(ent);
    }

    #endregion
}
