namespace Content.Shared._RedStar.Genetics.Events;

/// <summary>
/// Raised on an organism after its genes or their expression status change.
/// </summary>
[ByRefEvent]
public readonly record struct GenomeChangedEvent;
