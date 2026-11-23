using Content.Server._RedStar.Beekeeping.Components;
using Content.Shared.Botany.Components;
using Content.Shared.Mobs.Components;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// What a swarm will live on in its new home: plants like every colony, or the creatures around it.
/// </summary>
public sealed partial class MeatBeehiveSystem
{
    private readonly HashSet<Entity<PlantTrayComponent>> _dietTrays = new();
    private readonly HashSet<Entity<MobStateComponent>> _dietMobs = new();

    /// <summary>
    /// Looks around where the swarm comes out and picks its diet: only plants gives plants, only creatures gives meat,
    /// both is a draw weighted by how many of each, and neither leaves the plain plant swarm.
    /// </summary>
    public BeehiveDiet PickSwarmDiet(Entity<BeehiveComponent> hive, MapCoordinates center)
    {
        var comp = hive.Comp;
        return ChooseDiet(CountDietPlants(center, comp.DietRange),
            CountDietCreatures(hive, center, comp.DietRange),
            _random.NextFloat());
    }

    /// <summary>
    /// The diet for a swarm that can settle at any of several places: plants and creatures are counted around all of
    /// them together, so the type follows what is around the places it may end up at, not around the one the draw
    /// happened to take first.
    /// </summary>
    public BeehiveDiet PickSwarmDietAmong(Entity<BeehiveComponent> hive, IEnumerable<MapCoordinates> places)
    {
        var comp = hive.Comp;
        var plants = 0;
        var creatures = 0;
        foreach (var place in places)
        {
            plants += CountDietPlants(place, comp.DietRange);
            creatures += CountDietCreatures(hive, place, comp.DietRange);
        }

        return ChooseDiet(plants, creatures, _random.NextFloat());
    }

    /// <summary>
    /// Meat hives on the map and meat swarms still flying to build one, not counting <paramref name="except"/>.
    /// </summary>
    public int CountMeatHives(EntityUid? map, EntityUid? except = null)
    {
        var count = 0;
        var hives = EntityQueryEnumerator<BeehiveComponent, TransformComponent>();
        while (hives.MoveNext(out _, out var hive, out var xform))
        {
            if (hive.Wild && hive.Diet == BeehiveDiet.Meat && xform.MapUid == map)
                count++;
        }

        var swarms = EntityQueryEnumerator<BeeSwarmComponent, TransformComponent>();
        while (swarms.MoveNext(out var swarmUid, out var swarm, out var xform))
        {
            if (swarmUid != except && !swarm.Scatter && swarm.Diet == BeehiveDiet.Meat && xform.MapUid == map)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Plants win with nothing around or with plants only, meat with creatures only. With both, <paramref name="roll"/>
    /// (0 to 1) picks by their share: the more creatures, the likelier meat.
    /// </summary>
    public static BeehiveDiet ChooseDiet(int plants, int creatures, float roll)
    {
        if (creatures <= 0)
            return BeehiveDiet.Plants;

        if (plants <= 0)
            return BeehiveDiet.Meat;

        return roll * (plants + creatures) < creatures ? BeehiveDiet.Meat : BeehiveDiet.Plants;
    }

    /// <summary>
    /// Trays with a living plant in them.
    /// </summary>
    public int CountDietPlants(MapCoordinates center, float range)
    {
        _dietTrays.Clear();
        _lookup.GetEntitiesInRange(center, range, _dietTrays);
        var count = 0;
        foreach (var tray in _dietTrays)
        {
            if (_scout.GetLivingPlant(tray) != null)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Living creatures in range. Small ones (mice, cockroaches) are fast and frail and count only when there are
    /// at least <see cref="BeehiveComponent.MinSmallCreatures"/> of them.
    /// </summary>
    private int CountDietCreatures(Entity<BeehiveComponent> hive, MapCoordinates center, float range)
    {
        _dietMobs.Clear();
        _lookup.GetEntitiesInRange(center, range, _dietMobs);

        var large = 0;
        var small = 0;
        foreach (var mob in _dietMobs)
        {
            if (!_mobState.IsAlive(mob, mob.Comp) || _container.IsEntityInContainer(mob))
                continue;

            var mass = TryComp<PhysicsComponent>(mob, out var physics) ? physics.FixturesMass : float.MaxValue;
            if (mass < hive.Comp.SmallCreatureMass)
                small++;
            else
                large++;
        }

        return large + (small >= hive.Comp.MinSmallCreatures ? small : 0);
    }

    private readonly HashSet<Entity<MobStateComponent>> _quietMobs = new();

    /// <summary>
    /// Living creatures not inside anything within <paramref name="radius"/> of a point.
    /// </summary>
    public int CountLivingNear(MapCoordinates center, float radius)
    {
        _quietMobs.Clear();
        _lookup.GetEntitiesInRange(center, radius, _quietMobs);
        var count = 0;
        foreach (var mob in _quietMobs)
        {
            if (_mobState.IsAlive(mob, mob.Comp) && !_container.IsEntityInContainer(mob))
                count++;
        }

        return count;
    }
}
