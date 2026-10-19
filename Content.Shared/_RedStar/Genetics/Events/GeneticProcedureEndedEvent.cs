namespace Content.Shared._RedStar.Genetics.Events;

/// <summary>
/// Raised on the controlling console after chamber processing completes or is cancelled.
/// </summary>
[ByRefEvent]
public readonly record struct GeneticProcedureEndedEvent(EntityUid Chamber);
