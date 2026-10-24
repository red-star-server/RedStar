using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Data;
using Content.Server._RedStar.Beekeeping.Events;
using Content.Shared._RedStar.Beekeeping;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Spawners;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Inventory;
using Content.Shared.Hands;
using Content.Shared.FixedPoint;
using Content.Shared.Examine;
using Content.Shared.Destructible;
using Content.Shared.Damage.Systems;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Body.Systems;
using Content.Server.Stack;
using Content.Server.NPC.Systems;
using Content.Server.NPC.HTN;
using Content.Server.Atmos.Components;
using System.Linq;
using System.Numerics;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// Swarming. When a queen egg dies uncollected, the old queen leaves with half the bees and a young queen from the
/// egg stays in the hive with the rest. The swarm flies to a free locker, closet or crate it can reach in a straight
/// line and settles inside it (closing it hides the hive but not its hum); with none in sight it flies about looking
/// for one, like a lone queen. A wild hive works like a hive without frames: it keeps its combs inside, shows its
/// honey once it is half full and gives the combs only when it is broken. The number of wild hives on a map is
/// limited.
/// </summary>
public sealed partial class BeeSwarmSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private BeehiveSystem _hive = default!;
    [Dependency] private BeeErrandSystem _errands = default!;
    [Dependency] private MeatBeehiveSystem _meat = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private BeeScoutSystem _scout = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private TurfSystem _turf = default!;

    private readonly HashSet<Entity<BeehiveComponent>> _hivesNear = new();
    private readonly HashSet<EntityUid> _swarmTargets = new();
    private readonly HashSet<Entity<EntityStorageComponent>> _nestStorages = new();
    private readonly List<EntityUid> _storageCandidates = new();

    [Dependency] private SharedEntityStorageSystem _entityStorage = default!;

    /// <summary>
    /// The egg died with nobody collecting the jelly: the old queen swarms out, if the colony is big enough, a nest
    /// site is in reach and the map has room for one more wild hive. Returns whether a swarm left.
    /// </summary>
    public bool TrySwarm(Entity<BeehiveComponent> hive)
    {
        var comp = hive.Comp;
        if (comp.Bees < comp.SwarmMinBees ||
            _hive.GetQueen(hive) is not { } oldQueen ||
            !TryComp<BeeQueenComponent>(oldQueen, out var oldQueenComp))
        {
            return false;
        }

        var xform = Transform(hive);
        if (xform.GridUid is not { } grid ||
            !TryComp<MapGridComponent>(grid, out var gridComp) ||
            CountWildHives(xform.MapUid) >= _cfg.GetCVar(BeekeepingCVars.WildHiveLimit))
        {
            return false;
        }

        var hiveMap = _transform.GetMapCoordinates(hive, xform);
        var modifier = RollSwarmModifier();
        var range = comp.SwarmRange * modifier.RangeMultiplier;

        // A vent in sight that leads somewhere comes first: the swarm dives in and comes out elsewhere in the network.
        // Otherwise a free locker in sight, and with none the swarm sets off to look for one.
        EntityUid? storage = null;
        Vector2 site;
        var ventRoute = modifier.SkipVents ? null : FindSwarmVentRoute(hive, grid, hiveMap, range);

        // Only exits with a place to nest near them: a swarm that comes out where there is none just flies about.
        List<EntityUid>? nestExits = null;
        if (ventRoute is { } found)
        {
            nestExits = NestExits(found.Exits, range, grid);
            ventRoute = nestExits.Count > 0 ? (found.Entry, _random.Pick(nestExits), nestExits) : null;
        }

        if (ventRoute is { } route)
        {
            site = _transform.WithEntityId(Transform(route.Entry).Coordinates, grid).Position;
        }
        else
        {
            storage = PickNestStorage(hive, range, grid, hiveMap);
            site = storage is { } chosen
                ? _transform.WithEntityId(Transform(chosen).Coordinates, grid).Position
                : SearchHop(hive, hiveMap, (grid, gridComp), SearchHopLength);
        }

        if (_errands.Release(hive, xform, comp.SwarmPrototype, storage,
                ventRoute is { } way ? (way.Entry, way.Exit) : null,
                storage == null && ventRoute == null ? new EntityCoordinates(grid, site) : null) is not { } swarm ||
            !TryComp<BeeSwarmComponent>(swarm, out var swarmComp))
        {
            return false;
        }

        // The old queen goes with the swarm; a young one from the egg takes her place.
        var slot = _container.EnsureContainer<ContainerSlot>(swarm, swarmComp.Container);
        if (!_itemSlots.TryGetSlot(hive.Owner, comp.QueenSlot, out var queenSlot) ||
            queenSlot.ContainerSlot is not { } queenContainer ||
            !_container.Remove(oldQueen, queenContainer, force: true))
        {
            Del(swarm);
            return false;
        }

        if (!_container.Insert(oldQueen, slot, force: true))
        {
            // She could not be moved: she goes back into the hive and nothing swarms.
            _container.Insert(oldQueen, queenContainer, force: true);
            Del(swarm);
            return false;
        }

        var leaving = Math.Max(1, (int) (comp.Bees * modifier.ColonyShare));
        comp.Bees -= leaving;
        swarmComp.Bees = leaving;
        swarmComp.WildHive = comp.WildHivePrototype;
        swarmComp.Storage = storage;
        swarmComp.VentExit = ventRoute?.Exit;
        swarmComp.NestRange = range;
        swarmComp.Modifier = modifier.ID;
        // What the swarm lives on follows what is around the places it can settle at: all the exits that have a nest
        // near them, or, with no vents, around the hive.
        swarmComp.Diet = nestExits is { Count: > 0 }
            ? _meat.PickSwarmDietAmong(hive, nestExits.Select(vent => _transform.GetMapCoordinates(vent)))
            : _meat.PickSwarmDiet(hive, hiveMap);

        // Past the limit of meat hives on the map the swarm is a plain plant one.
        if (swarmComp.Diet == BeehiveDiet.Meat && _meat.CountMeatHives(xform.MapUid, swarm) >= _cfg.GetCVar(BeekeepingCVars.MeatHiveLimit))
            swarmComp.Diet = BeehiveDiet.Plants;

        // Among the exits with a nest near them: a plant swarm takes the one with the most plants around, a meat swarm
        // the quietest one and the quietest place near it, since a hive found right away is a hive broken right away.
        if (swarmComp.Diet == BeehiveDiet.Plants && nestExits is { Count: > 0 } && RichestExit(nestExits, comp.DietRange) is { } plantExit)
            swarmComp.VentExit = plantExit;

        if (swarmComp.Diet == BeehiveDiet.Meat)
        {
            if (nestExits is { Count: > 0 } && QuietestExit(nestExits, QuietRadius) is { } quietExit)
                swarmComp.VentExit = quietExit;
            else if (ventRoute == null && PickNestStorage(hive, range, grid, hiveMap, QuietRadius) is { } quietStorage)
            {
                // It flies to the place it settles in, not to the first one it saw.
                swarmComp.Storage = quietStorage;
                _errands.SetTarget(swarm, quietStorage);
            }
        }

        // The exit was changed for the diet: the route goes there.
        if (ventRoute is { } chosenRoute && swarmComp.VentExit is { } exit)
            _errands.SetRoute(swarm, chosenRoute.Entry, exit);

        swarmComp.Searching = ventRoute == null && storage == null;

        var daughter = Spawn(comp.DaughterQueenPrototype, xform.Coordinates);
        if (TryComp<BeeQueenComponent>(daughter, out var daughterComp))
            InheritTraits(oldQueenComp, daughterComp);
        if (!_itemSlots.TryInsert(hive.Owner, comp.QueenSlot, daughter, null))
        {
            // The slot was just emptied, so this should not happen; the young queen is not lost if it does.
            Log.Warning($"{ToPrettyString(hive)} could not take its young queen after swarming");
            _transform.DropNextTo(daughter, hive.Owner);
        }

        if (swarmComp.Diet == BeehiveDiet.Meat)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(hive):hive} released a meat swarm that will live on blood");
        }

        _hive.UpdateAmbience(hive);
        _popup.PopupEntity(Loc.GetString("beekeeping-hive-swarm"), hive);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(hive):hive} swarmed: the old queen left with {leaving} bees");
        return true;
    }

    /// <summary>
    /// One quirk for the swarm, by weight. Without any prototypes a plain swarm.
    /// </summary>
    private SwarmModifierPrototype RollSwarmModifier()
    {
        var weights = new Dictionary<SwarmModifierPrototype, float>();
        foreach (var modifier in _proto.EnumeratePrototypes<SwarmModifierPrototype>())
        {
            if (modifier.Weight > 0f)
                weights[modifier] = modifier.Weight;
        }

        return weights.Count > 0 ? _random.Pick(weights) : _proto.Index(PlainSwarm);
    }

    /// <summary>
    /// Extra bees of a crowded hive leave together: they fly off as a swarm with no queen and scatter. The colony
    /// loses them at once.
    /// </summary>
    public void LeaveInSwarm(Entity<BeehiveComponent> hive, int count)
    {
        var comp = hive.Comp;
        count = Math.Min(count, comp.Bees);
        if (count <= 0)
            return;

        for (var i = 0; i < count; i++)
        {
            _hive.RemoveHomeBee(hive);
        }

        var xform = Transform(hive);
        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        // Off in a random direction, as far as it can fly in a straight line, up to the scouting range.
        var hiveMap = _transform.GetMapCoordinates(hive, xform);
        var angle = _random.NextFloat() * MathF.Tau;
        var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var distance = Math.Min(comp.ScoutRange, _scout.FreeFlight(hiveMap, direction, comp.ScoutRange, hive) - 0.5f);
        var target = _map.WorldToLocal(grid, gridComp, hiveMap.Position + direction * Math.Max(distance, 0.5f));
        if (_errands.Release(hive, xform, comp.LeavingSwarmPrototype, null, null, new EntityCoordinates(grid, target)) is not { } swarm ||
            !TryComp<BeeSwarmComponent>(swarm, out var swarmComp))
        {
            return;
        }

        swarmComp.Scatter = true;
        swarmComp.Bees = count;
        _popup.PopupEntity(Loc.GetString("beekeeping-hive-crowd-swarm"), hive);
    }

    /// <summary>
    /// The young queen has her mother's traits, as many as her slots hold. Innate traits always pass on.
    /// </summary>
    private void InheritTraits(BeeQueenComponent mother, BeeQueenComponent daughter)
    {
        daughter.Traits.Clear();
        var slotted = 0;
        foreach (var id in mother.Traits)
        {
            if (!_proto.Resolve(id, out var trait))
                continue;

            if (!trait.Innate)
            {
                if (slotted >= daughter.Slots)
                    continue;

                slotted++;
            }

            daughter.Traits.Add(id);
        }
    }

    /// <summary>
    /// A random locker, closet or crate in range on the grid that a swarm from <paramref name="origin"/> can fly to
    /// and that has room. Null if there is none.
    /// </summary>
    public EntityUid? PickNestStorage(EntityUid origin, float range, EntityUid grid, MapCoordinates hiveMap,
        float quietRadius = 0f)
    {
        _nestStorages.Clear();
        _lookup.GetEntitiesInRange(hiveMap, range, _nestStorages);
        if (_nestStorages.Count == 0)
            return null;

        // What makes a place taken is gathered once for the whole search: the hives around and the lockers swarms are
        // already flying to.
        _hivesNear.Clear();
        _lookup.GetEntitiesInRange(hiveMap, range + 1f, _hivesNear);
        _swarmTargets.Clear();
        var swarms = EntityQueryEnumerator<BeeSwarmComponent>();
        while (swarms.MoveNext(out _, out var swarm))
        {
            if (swarm.Storage is { } target)
                _swarmTargets.Add(target);
        }

        _storageCandidates.Clear();
        foreach (var storage in _nestStorages)
        {
            var xform = Transform(storage);
            if (xform.GridUid != grid ||
                _container.IsEntityInContainer(storage) ||
                storage.Comp.Contents.ContainedEntities.Count >= storage.Comp.Capacity ||
                NestTaken(storage, xform) ||
                !_scout.ClearFlightPath(hiveMap, _transform.GetMapCoordinates(storage, xform), origin, storage))
            {
                continue;
            }

            _storageCandidates.Add(storage);
        }

        if (_storageCandidates.Count == 0)
            return null;

        // The quietest ones: those with the fewest living creatures around them.
        if (quietRadius > 0f)
        {
            var fewest = int.MaxValue;
            var counts = new List<int>(_storageCandidates.Count);
            foreach (var candidate in _storageCandidates)
            {
                var count = _meat.CountLivingNear(_transform.GetMapCoordinates(candidate), quietRadius);
                counts.Add(count);
                fewest = Math.Min(fewest, count);
            }

            for (var i = _storageCandidates.Count - 1; i >= 0; i--)
            {
                if (counts[i] > fewest)
                    _storageCandidates.RemoveAt(i);
            }
        }

        return _random.Pick(_storageCandidates);
    }

    /// <summary>
    /// A locker or crate already holding a hive, or one a swarm is already flying to, is somebody's home.
    /// </summary>
    private bool NestTaken(Entity<EntityStorageComponent> storage, TransformComponent storageXform)
    {
        if (_swarmTargets.Contains(storage.Owner))
            return true;

        foreach (var contained in storage.Comp.Contents.ContainedEntities)
        {
            if (HasComp<BeehiveComponent>(contained))
                return true;
        }

        // An open locker keeps its hive on top of it rather than inside.
        foreach (var hive in _hivesNear)
        {
            var hiveXform = Transform(hive);
            if (hiveXform.ParentUid == storageXform.ParentUid &&
                (hiveXform.LocalPosition - storageXform.LocalPosition).Length() < 0.5f)
            {
                return true;
            }
        }

        return false;
    }

    public const float SearchHopLength = 5f;

    /// <summary>
    /// What a swarm or a lone queen does where its errand took it: bees with no queen scatter, a swarm out of the pipes
    /// looks for a place near the exit, a searching one looks around and flies on, and one at its site builds the hive.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnArrived(Entity<BeeSwarmComponent> ent, ref BeeErrandArrivedEvent args)
    {
        args.Handled = true;
        if (ent.Comp.Scatter)
        {
            args.Result = BeeErrandArrival.Gone;
            return;
        }

        if (ent.Comp.VentExit != null)
        {
            SwarmLeftVent(ent);
            args.Result = BeeErrandArrival.KeepGoing;
            return;
        }

        if (ent.Comp.Searching)
        {
            args.Result = ContinueSearch(ent) ? BeeErrandArrival.KeepGoing : BeeErrandArrival.Gone;
            return;
        }

        LandSwarm(ent);
        args.Result = BeeErrandArrival.Gone;
    }

    /// <summary>
    /// The swarm with no quirk, for a server with no other quirks in its data.
    /// </summary>
    private static readonly ProtoId<SwarmModifierPrototype> PlainSwarm = "Plain";

    /// <summary>
    /// Where a searching swarm or queen flies next: a few tries for a random direction with room to fly, failing that
    /// it stays put and tries again next time. Grid coordinates.
    /// </summary>
    public Vector2 SearchHop(EntityUid flyer, MapCoordinates origin, Entity<MapGridComponent> grid, float length)
    {
        var direction = Vector2.UnitX;
        var distance = 0f;
        for (var i = 0; i < 4 && distance < 1f; i++)
        {
            var angle = _random.NextFloat() * MathF.Tau;
            direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            distance = Math.Min(length, _scout.FreeFlight(origin, direction, length, flyer) - 0.5f);
        }

        return _map.WorldToLocal(grid, grid.Comp, origin.Position + direction * Math.Max(distance, 0f));
    }

    /// <summary>
    /// A swarm or a lone queen searching for a home finished a hop: if a locker or crate is in sight now she heads there,
    /// otherwise she hops on in a random direction. False if she has nothing to search with any more.
    /// </summary>
    public bool ContinueSearch(Entity<BeeSwarmComponent> swarm)
    {
        var xform = Transform(swarm);
        if (xform.GridUid is not { } grid ||
            !TryComp<MapGridComponent>(grid, out var gridComp) ||
            !_container.TryGetContainer(swarm, swarm.Comp.Container, out var container) ||
            container.ContainedEntities.Count == 0 ||
            !TryComp<BeeQueenComponent>(container.ContainedEntities[0], out var queen))
        {
            return false;
        }

        var origin = _transform.GetMapCoordinates(swarm, xform);
        if (PickNestStorage(swarm, queen.NestRange, grid, origin) is { } storage)
        {
            swarm.Comp.Searching = false;
            swarm.Comp.Storage = storage;
            _errands.SetTarget(swarm.Owner, storage);
            return true;
        }

        var hop = SearchHop(swarm, origin, (grid, gridComp), swarm.Comp.SearchHop);
        _errands.SetTarget(swarm.Owner, null, new EntityCoordinates(grid, hop));
        return true;
    }

    public int CountWildHives(EntityUid? map)
    {
        var count = 0;
        var query = EntityQueryEnumerator<BeehiveComponent, TransformComponent>();
        while (query.MoveNext(out _, out var hive, out var xform))
        {
            if (hive.Wild && xform.MapUid == map)
                count++;
        }

        return count;
    }

    /// <summary>
    /// The swarm reached its site: a wild hive grows there with the old queen and the bees that came along.
    /// </summary>
    public void LandSwarm(Entity<BeeSwarmComponent> swarm)
    {
        var xform = Transform(swarm);
        var wild = Spawn(swarm.Comp.WildHive, xform.Coordinates);
        if (!TryComp<BeehiveComponent>(wild, out var wildComp) ||
            !_container.TryGetContainer(swarm, swarm.Comp.Container, out var container))
        {
            Del(wild);
            DropSwarmQueen(swarm);
            return;
        }

        foreach (var queen in new List<EntityUid>(container.ContainedEntities))
        {
            _container.Remove(queen, container, force: true);
            if (!_itemSlots.TryInsert(wild, wildComp.QueenSlot, queen, null))
                _transform.DropNextTo(queen, wild);
        }

        // Into the storage it chose, if it is still there and has room; otherwise the hive stays on the floor.
        if (swarm.Comp.Storage is { } storage && !TerminatingOrDeleted(storage))
            _entityStorage.Insert(wild, storage);

        // A lone queen starts the colony like a new hive does; a swarm brings its bees.
        if (swarm.Comp.Bees > 0)
            wildComp.Bees = swarm.Comp.Bees;
        wildComp.ScoutState = BeeScoutState.Home;
        wildComp.Diet = swarm.Comp.Diet;
        if (wildComp.Diet == BeehiveDiet.Meat)
        {
            _adminLog.Add(LogType.EntitySpawn, LogImpact.Medium,
                $"A meat bee swarm built {ToPrettyString(wild):hive} at {xform.Coordinates}");
        }
        if (swarm.Comp.Modifier is { } modifierId && _proto.TryIndex<SwarmModifierPrototype>(modifierId, out var modifier))
        {
            wildComp.SwarmModifier = modifierId;
            if (modifier.WildCombs > 0)
                wildComp.MaxWildCombs = modifier.WildCombs;
        }

        _hive.UpdateMaxBees((wild, wildComp));
        _hive.UpdateAmbience((wild, wildComp));
        _hive.UpdateHiveAppearance((wild, wildComp));
        swarm.Comp.Bees = 0;

        _adminLog.Add(LogType.EntitySpawn, LogImpact.Low,
            $"A bee swarm built {ToPrettyString(wild):hive} with {wildComp.Bees} bees");
    }

    /// <summary>
    /// A swarm that is gone before it lands leaves its queen where it was.
    /// </summary>
    private void DropSwarmQueen(Entity<BeeSwarmComponent> swarm)
    {
        if (!_container.TryGetContainer(swarm, swarm.Comp.Container, out var container))
            return;

        foreach (var queen in new List<EntityUid>(container.ContainedEntities))
        {
            _container.Remove(queen, container, force: true);
            _transform.DropNextTo(queen, swarm.Owner);
        }
    }

    [SubscribeLocalEvent]
    private void OnSwarmTerminating(Entity<BeeSwarmComponent> ent, ref EntityTerminatingEvent args)
    {
        var xform = Transform(ent);
        if (TerminatingOrDeleted(xform.ParentUid))
            return;

        DropSwarmQueen(ent);
    }

    /// <summary>
    /// A wild hive gives its combs only when it is broken; a tainted one's combs carry something from the botanists'
    /// reagent list.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBeehiveBroken(ref BeehiveBrokenEvent args)
    {
        if (!TryComp<BeehiveComponent>(args.Hive, out var comp) || !comp.Wild)
            return;

        if (comp.WildCombs > 0)
        {
            var combs = SpawnNextToOrDrop(comp.WildCombPrototype, args.Hive);
            if (TryComp<StackComponent>(combs, out var stack))
                _stack.SetCount((combs, stack), comp.WildCombs);

            var dropped = new WildCombsDroppedEvent(args.Hive, combs, comp.WildCombs);
            RaiseLocalEvent(ref dropped);

            if (SwarmModifierOf(comp)?.CombReagent is { } fill &&
                _solutions.TryGetSolution(combs, "food", out var combSolution, out _))
            {
                var pick = _proto.Index(fill).Pick(_random);
                _solutions.ForceAddSolution(combSolution.Value, new Solution(pick.reagent, pick.quantity));
            }
        }

        comp.WildCombs = 0;
    }

    private SwarmModifierPrototype? SwarmModifierOf(BeehiveComponent comp)
    {
        return comp.SwarmModifier is { } id && _proto.TryIndex(id, out var modifier) ? modifier : null;
    }
}
