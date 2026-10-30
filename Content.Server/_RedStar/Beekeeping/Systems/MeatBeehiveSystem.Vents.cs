using Content.Server.Atmos.Piping.Unary.Components;
using Content.Shared.Mobs.Components;
using Content.Shared._RedStar.Beekeeping.Components;
using Robust.Shared.Map;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// The meat colony's way through the pipes: a forager with no straight way to a creature dives into a vent in sight of
/// the hive and comes out of another vent of the same network near the creature.
/// </summary>
public sealed partial class MeatBeehiveSystem
{
    private readonly HashSet<Entity<GasVentPumpComponent>> _huntVents = new();
    private readonly List<Entity<GasVentPumpComponent>> _routeEntries = new();
    private readonly HashSet<Entity<MobStateComponent>> _viaFound = new();
    private readonly Dictionary<EntityUid, List<(EntityUid Entry, EntityUid Exit)>> _viaExits = new();
    private readonly Stack<List<(EntityUid Entry, EntityUid Exit)>> _viaPool = new();

    /// <summary>
    /// A way for a meat forager to reach <paramref name="target"/> through the pipes: a vent in sight of the hive to dive
    /// into and, in the same network, the nearest vent that is within the hunting range of the target and has a clear
    /// line to it. Null if there is none. Welded vents do not count.
    /// </summary>
    public (EntityUid Entry, EntityUid Exit)? FindHuntRoute(Entity<BeehiveComponent> hive, MapCoordinates hiveMap, EntityUid target)
    {
        var meatComp = MeatOf(hive);
        var grid = Transform(hive).GridUid;
        var targetMap = _transform.GetMapCoordinates(target);
        var range = meatComp.VentHuntRange;

        _huntVents.Clear();
        _lookup.GetEntitiesInRange(hiveMap, hive.Comp.VisitRange, _huntVents);
        _routeEntries.Clear();
        _routeEntries.AddRange(_huntVents);
        foreach (var entry in _routeEntries)
        {
            if (TerminatingOrDeleted(entry) ||
                Transform(entry).GridUid != grid ||
                _weldable.IsWelded(entry) ||
                !_scout.ClearFlightPath(hiveMap, _transform.GetMapCoordinates(entry), hive, entry))
            {
                continue;
            }

            if (NetworkVents(entry) is not { } vents)
                continue;

            EntityUid? best = null;
            var bestDistance = float.MaxValue;
            foreach (var exit in vents)
            {
                var exitMap = _transform.GetMapCoordinates(exit);
                var distance = (exitMap.Position - targetMap.Position).Length();
                if (distance > range || distance >= bestDistance || !_scout.ClearFlightPath(exitMap, targetMap, exit, target))
                    continue;

                best = exit;
                bestDistance = distance;
            }

            if (best is { } found)
                return (entry.Owner, found);
        }

        return null;
    }

    /// <summary>
    /// Every vent of the entry's network that a bee could come out of: not welded, on the same map. Reused list.
    /// </summary>
    private List<EntityUid>? NetworkVents(Entity<GasVentPumpComponent> entry)
    {
        var vents = _swarm.NetworkExits(entry);
        return vents.Count > 0 ? vents : null;
    }

    /// <summary>
    /// For each vent a meat forager can dive into from the hive, the vents of its network within reach, and the
    /// creatures around each of them, added to the ones already in <c>_creatures</c>. Every creature found keeps the
    /// (entry, exit) pairs it was found from in <c>_viaExits</c>, so choosing a target needs no further search of the
    /// network: one pass over the vents, one look around each.
    /// </summary>
    public void GatherCreaturesNearVents(Entity<BeehiveComponent> hive, MapCoordinates hiveMap)
    {
        var meatComp = MeatOf(hive);
        foreach (var list in _viaExits.Values)
        {
            list.Clear();
            _viaPool.Push(list);
        }

        _viaExits.Clear();

        var grid = Transform(hive).GridUid;
        _huntVents.Clear();
        _lookup.GetEntitiesInRange(hiveMap, hive.Comp.VisitRange, _huntVents);
        _routeEntries.Clear();
        _routeEntries.AddRange(_huntVents);
        foreach (var entry in _routeEntries)
        {
            if (TerminatingOrDeleted(entry) ||
                Transform(entry).GridUid != grid ||
                _weldable.IsWelded(entry) ||
                !_scout.ClearFlightPath(hiveMap, _transform.GetMapCoordinates(entry), hive, entry) ||
                NetworkVents(entry) is not { } vents)
            {
                continue;
            }

            foreach (var exit in vents)
            {
                var exitMap = _transform.GetMapCoordinates(exit);
                if ((exitMap.Position - hiveMap.Position).Length() > meatComp.VentHuntReach)
                    continue;

                _viaFound.Clear();
                _lookup.GetEntitiesInRange(exitMap, meatComp.VentHuntRange, _viaFound);
                foreach (var mob in _viaFound)
                {
                    _creatures.Add(mob);
                    if (!_viaExits.TryGetValue(mob.Owner, out var list))
                    {
                        list = _viaPool.Count > 0 ? _viaPool.Pop() : new List<(EntityUid, EntityUid)>();
                        _viaExits[mob.Owner] = list;
                    }

                    list.Add((entry.Owner, exit));
                }
            }
        }
    }

    /// <summary>
    /// The first of the (entry, exit) pairs a creature was found from whose exit has a clear line to it. Null if none.
    /// </summary>
    public (EntityUid Entry, EntityUid Exit)? ClearVentRoute(EntityUid creature, MapCoordinates creatureMap)
    {
        if (!_viaExits.TryGetValue(creature, out var pairs))
            return null;

        foreach (var (entry, exit) in pairs)
        {
            if (!TerminatingOrDeleted(exit) &&
                _scout.ClearFlightPath(_transform.GetMapCoordinates(exit), creatureMap, exit, creature))
            {
                return (entry, exit);
            }
        }

        return null;
    }
}
