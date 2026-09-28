using Content.Shared._Starlight.Xenobiology;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Xenobiology;

public sealed partial class SlimeBrainSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;

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
    public readonly float FoodSearchRange = 5F;

    /// <summary>
    /// If not null, will only allow slimes to eat entities with the specified damage container.
    /// If null, will make slimes try to eat everything.
    /// </summary>
    public readonly ProtoId<DamageContainerPrototype>? OnlyTarget = "Biological";

    public bool IsEdibleBySlimeTest(EntityUid entity)
    {
        // Don't cannibalize other slimes
        if (HasComp<SlimeComponent>(entity)) return false;

        if (!HasComp<DamageableComponent>(entity)) return false;

        // Don't target entities that aren't mobs
        if (!HasComp<MobStateComponent>(entity)) return false;

        // Don't target entities in the wrong damage group
        if (!OnlyTarget.HasValue) return _mobState.IsAlive(entity);
        if (!TryComp<InjurableComponent>(entity, out var injurable) ||
            injurable.DamageContainer != OnlyTarget.Value)
            return false;

        // Only target living entities.
        return _mobState.IsAlive(entity);
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
    /// <returns>The set of valid food targets. May be empty.</returns>
    public HashSet<EntityUid> AcquireTargetFoods()
    {
        HashSet<EntityUid> targetsToReturn = new();
        HashSet<EntityUid> targetsToDelete = new();
        foreach (var possibleTarget in _targetFood)
        {
            if (IsEdibleBySlimeTest(possibleTarget))
            {
                targetsToReturn.Add(possibleTarget);
            }
            else
            {
                targetsToDelete.Add(possibleTarget);
            }
        }
        foreach (var delete in targetsToDelete)
        {
            _targetFood.Remove(delete);
        }
        return targetsToReturn;
    }

    /// <summary>
    /// Adds a given coordinate to the known feeding spots set.
    /// </summary>
    /// <param name="coordinates">The feeding spot location to add.</param>
    public void AddFeedingSpot(EntityCoordinates coordinates) => _knownFoodLocations.Add(coordinates);

    /// <summary>
    /// Retrieves the set of feeding spots known to the slime brain.
    /// </summary>
    /// <returns>The set of feeding spots.</returns>
    public HashSet<EntityCoordinates> AcquireFeedingSpots()
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
