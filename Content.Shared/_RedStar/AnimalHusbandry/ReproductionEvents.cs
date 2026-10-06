namespace Content.Shared.Nutrition.AnimalHusbandry;

/// <summary>
/// Allows an entity to block a reproduction attempt before resources are spent.
/// </summary>
public sealed class ReproductionAttemptEvent : CancellableEntityEventArgs;

/// <summary>
/// Allows an entity to defer a pending birth before offspring are spawned.
/// </summary>
public sealed class BirthAttemptEvent : CancellableEntityEventArgs;

/// <summary>
/// Raised after all offspring have spawned and the carrier's gestation has reset.
/// </summary>
[ByRefEvent]
public readonly record struct BirthCompletedEvent(IReadOnlyList<EntityUid> Offspring);
