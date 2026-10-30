using Content.Server._RedStar.Beekeeping.Components;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.NodeContainer;
using Content.Shared.Tools.Systems;
using Content.Shared._RedStar.Beekeeping.Data;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// A swarm can leave through the air vents: it dives into a vent it can see and comes out of another vent of the same
/// pipe network (a welded vent blocks both ways); the dive through the pipes is the errand's (<see cref="BeeErrandSystem"/>). Only vent pumps, not scrubbers. From the exit it looks for a place
/// to nest, as a swarm does anywhere else.
/// </summary>
public sealed partial class BeeSwarmSystem
{
    [Dependency] private WeldableSystem _weldable = default!;

    /// <summary>
    /// How far around a place a meat swarm counts the living creatures when it looks for a quiet one, in tiles.
    /// </summary>
    private const float QuietRadius = 8f;

    private readonly HashSet<Entity<GasVentPumpComponent>> _ventsNear = new();
    private readonly List<Entity<GasVentPumpComponent>> _ventCandidates = new();
    private readonly List<EntityUid> _ventExits = new();
    private readonly Dictionary<INodeGroup, List<EntityUid>> _networkCache = new();

    /// <summary>
    /// A vent in sight of <paramref name="origin"/> on its grid and another vent of the same network on the same map
    /// to come out of, with all the vents of that network it could come out of. Null if there is no such pair. Each
    /// network is gone through once, however many of its vents are in sight.
    /// </summary>
    public (EntityUid Entry, EntityUid Exit, List<EntityUid> Exits)? FindSwarmVentRoute(EntityUid origin,
        EntityUid grid,
        MapCoordinates originMap,
        float range)
    {
        _ventsNear.Clear();
        _lookup.GetEntitiesInRange(originMap, range, _ventsNear);

        _ventCandidates.Clear();
        _networkCache.Clear();
        foreach (var vent in _ventsNear)
        {
            if (TerminatingOrDeleted(vent) ||
                Transform(vent).GridUid != grid ||
                _weldable.IsWelded(vent) ||
                !_scout.ClearFlightPath(originMap, _transform.GetMapCoordinates(vent), origin, vent) ||
                CachedNetwork(vent) is not { } network ||
                !network.Exists(other => other != vent.Owner))
            {
                continue;
            }

            _ventCandidates.Add(vent);
        }

        if (_ventCandidates.Count == 0)
            return null;

        var entry = _random.Pick(_ventCandidates);
        var exits = CachedNetwork(entry)!.FindAll(other => other != entry.Owner);
        return (entry.Owner, _random.Pick(exits), exits);
    }

    /// <summary>
    /// Every usable vent of the vent's network, the vent itself included, worked out once per search.
    /// </summary>
    private List<EntityUid>? CachedNetwork(Entity<GasVentPumpComponent> vent)
    {
        if (!TryComp<NodeContainerComponent>(vent, out var nodes) ||
            !nodes.Nodes.TryGetValue(vent.Comp.Inlet, out var node) ||
            node.NodeGroup is not { } group)
        {
            return null;
        }

        if (_networkCache.TryGetValue(group, out var cached))
            return cached;

        FindNetworkVents(vent);
        var all = new List<EntityUid>(_ventExits) { vent.Owner };
        _networkCache[group] = all;
        return all;
    }

    /// <summary>
    /// The exits of the entry's network from which a swarm can find a place to nest: a free locker or crate within
    /// <paramref name="range"/> in clear sight of the vent. A swarm that comes out anywhere else only flies about.
    /// </summary>
    public List<EntityUid> NestExits(EntityUid entry, float range, EntityUid grid)
    {
        if (!TryComp<GasVentPumpComponent>(entry, out var pump))
            return new List<EntityUid>();

        FindNetworkVents((entry, pump));
        return NestExits(new List<EntityUid>(_ventExits), range, grid);
    }

    /// <summary>
    /// Of the given exits, those with a place to nest near them.
    /// </summary>
    public List<EntityUid> NestExits(List<EntityUid> exits, float range, EntityUid grid)
    {
        var nest = new List<EntityUid>();
        foreach (var vent in exits)
        {
            if (PickNestStorage(vent, range, grid, _transform.GetMapCoordinates(vent)) != null)
                nest.Add(vent);
        }

        return nest;
    }

    /// <summary>
    /// Of the given exits, one with the most living plants around it, by chance among equals. Null if none has any,
    /// which keeps the choice as it was.
    /// </summary>
    private EntityUid? RichestExit(List<EntityUid> exits, float radius)
    {
        var most = 0;
        var counts = new List<int>(exits.Count);
        foreach (var vent in exits)
        {
            var count = _meat.CountDietPlants(_transform.GetMapCoordinates(vent), radius);
            counts.Add(count);
            most = Math.Max(most, count);
        }

        if (most <= 0)
            return null;

        var best = new List<EntityUid>();
        for (var i = 0; i < exits.Count; i++)
        {
            if (counts[i] == most)
                best.Add(exits[i]);
        }

        return _random.Pick(best);
    }

    /// <summary>
    /// Of the given exits, the one with the fewest living creatures around it, by chance among equals.
    /// </summary>
    private EntityUid? QuietestExit(List<EntityUid> exits, float radius)
    {
        if (exits.Count == 0)
            return null;

        var fewest = int.MaxValue;
        var counts = new List<int>(exits.Count);
        foreach (var vent in exits)
        {
            var count = _meat.CountLivingNear(_transform.GetMapCoordinates(vent), radius);
            counts.Add(count);
            fewest = Math.Min(fewest, count);
        }

        var best = new List<EntityUid>();
        for (var i = 0; i < exits.Count; i++)
        {
            if (counts[i] == fewest)
                best.Add(exits[i]);
        }

        return _random.Pick(best);
    }

    /// <summary>
    /// The quietest other vent of the entry's network, whether or not it has a nest nearby.
    /// </summary>
    public EntityUid? QuietestExit(EntityUid entry, float radius)
    {
        if (!TryComp<GasVentPumpComponent>(entry, out var pump))
            return null;

        FindNetworkVents((entry, pump));
        return QuietestExit(new List<EntityUid>(_ventExits), radius);
    }

    /// <summary>
    /// Every other vent pump of the entry's network a bee could come out of: not welded, on the same map. A new list.
    /// </summary>
    public List<EntityUid> NetworkExits(Entity<GasVentPumpComponent> entry)
    {
        FindNetworkVents(entry);
        return new List<EntityUid>(_ventExits);
    }

    /// <summary>
    /// Fills <c>_ventExits</c> with every other vent pump of the entry's pipe network that is not welded and is on
    /// its map. It goes through the vent pumps (a few dozen on a station) and compares their networks, not through
    /// every pipe of the network (thousands).
    /// </summary>
    private void FindNetworkVents(Entity<GasVentPumpComponent> entry)
    {
        _ventExits.Clear();
        if (!TryComp<NodeContainerComponent>(entry, out var nodes) ||
            !nodes.Nodes.TryGetValue(entry.Comp.Inlet, out var node) ||
            node.NodeGroup is not { } group)
        {
            return;
        }

        var map = Transform(entry).MapID;
        var vents = EntityQueryEnumerator<GasVentPumpComponent, NodeContainerComponent, TransformComponent>();
        while (vents.MoveNext(out var uid, out var pump, out var ventNodes, out var xform))
        {
            if (uid == entry.Owner ||
                xform.MapID != map ||
                !ventNodes.Nodes.TryGetValue(pump.Inlet, out var ventNode) ||
                ventNode.NodeGroup != group ||
                TerminatingOrDeleted(uid) ||
                _weldable.IsWelded(uid))
            {
                continue;
            }

            _ventExits.Add(uid);
        }
    }

    /// <summary>
    /// Out of the vent: look for a locker or crate around the exit, or search on.
    /// </summary>
    public void SwarmLeftVent(Entity<BeeSwarmComponent> swarm)
    {
        swarm.Comp.VentExit = null;

        var xform = Transform(swarm);
        if (xform.GridUid is not { } grid)
            return;

        var origin = _transform.GetMapCoordinates(swarm, xform);
        if (PickNestStorage(swarm, swarm.Comp.NestRange, grid, origin, swarm.Comp.Diet == BeehiveDiet.Meat ? QuietRadius : 0f) is { } storage)
        {
            swarm.Comp.Storage = storage;
            swarm.Comp.Searching = false;
            _errands.SetTarget(swarm.Owner, storage);
            return;
        }

        swarm.Comp.Searching = true;
        ContinueSearch(swarm);
    }
}
