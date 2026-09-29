using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Xenobiology.Slimes.Systems;

public sealed partial class SlimeBrainSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;

    [Dependency] private EntityQuery<SlimeComponent> _slimeQuery;
    [Dependency] private EntityQuery<DamageableComponent> _damageableQuery;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery;
    [Dependency] private EntityQuery<InjurableComponent> _injurableQuery;

    private readonly Predicate<EntityUid> _invalidFoodPredicate;

    public SlimeBrainSystem()
    {
        _invalidFoodPredicate = entity => !IsEdibleBySlimeTest(entity);
    }

    /// <summary>
    /// The set of food targets slimes can safely eat.
    /// </summary>
    private readonly HashSet<EntityUid> _targetFood = [];

    /// <summary>
    /// The locations marked by slimes indicating there may be food nearby.
    /// Specifically, if a slime eats a monkey at a spot, they will mark it as a known food location.
    /// If a slime arrived to the spot and doesn't find any food to eat, they will un-mark it.
    /// </summary>
    private readonly HashSet<EntityCoordinates> _knownFoodLocations = [];

    /// <summary>
    /// How far to look for food at each slime.
    /// </summary>
    public const float FoodSearchRange = 5f;

    /// <summary>
    /// Only entities with this damage container can be eaten by slimes.
    /// </summary>
    private static readonly ProtoId<DamageContainerPrototype> OnlyTarget = "Biological";

    public bool IsEdibleBySlimeTest(EntityUid entity)
    {
        if (TerminatingOrDeleted(entity) || EntityManager.IsQueuedForDeletion(entity) ||
            _slimeQuery.HasComp(entity) || !_damageableQuery.HasComp(entity) ||
            !_mobStateQuery.TryComp(entity, out var mobState))
            return false;

        if (!_injurableQuery.TryComp(entity, out var injurable) || injurable.DamageContainer != OnlyTarget)
            return false;

        return _mobState.IsAlive(entity, mobState);
    }

    /// <summary>
    /// Attempts to add a food target to the slime brain. Targets are only added if they pass the edible slime test (see <see cref="IsEdibleBySlimeTest"/>).
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>Returns true if successful, returns false if the target food fails the test and thus cannot be added.</returns>
    public bool TryAddTargetFood(EntityUid entity)
    {
        if (!IsEdibleBySlimeTest(entity))
            return false;

        _targetFood.Add(entity);
        return true;

    }

    /// <summary>
    /// Grabs the set of valid food targets that are known to the brain
    /// </summary>
    /// <returns>A live read-only view of valid food targets. May be empty; not a snapshot.</returns>
    public IReadOnlySet<EntityUid> AcquireTargetFoods()
    {
        _targetFood.RemoveWhere(_invalidFoodPredicate);
        return _targetFood;
    }

    /// <summary>
    /// Adds a given coordinate to the known feeding spots set.
    /// </summary>
    /// <param name="coordinates">The feeding spot location to add.</param>
    public void AddFeedingSpot(EntityCoordinates coordinates) => _knownFoodLocations.Add(coordinates);

    /// <summary>
    /// Retrieves the set of feeding spots known to the slime brain.
    /// </summary>
    /// <returns>A read-only snapshot, safe to enumerate across asynchronous pathfinding.</returns>
    public IReadOnlySet<EntityCoordinates> AcquireFeedingSpots()
    {
        HashSet<EntityCoordinates> coordsToReturn = [.. _knownFoodLocations];

        return coordsToReturn;
    }

    /// <summary>
    /// Called by slimes if they successfully eat food.
    /// </summary>
    /// <param name="entity">The slime entity.</param>
    public void SlimeSuccessfulEat(EntityUid entity)
        => _knownFoodLocations.Add(Transform(entity).Coordinates);

    /// <summary>
    /// Called by slimes if they couldn't find anything nearby to eat.
    /// </summary>
    /// <param name="entity">The slime entity.</param>
    public void SlimeUnsuccessfulFoodFind(EntityUid entity)
        => _knownFoodLocations.Remove(Transform(entity).Coordinates);
}
