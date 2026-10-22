namespace Content.Shared._RedStar.Genetics.Events;

/// <summary>
/// Raised on an organism and broadcast after its genes change or its genome is removed.
/// </summary>
[ByRefEvent]
public readonly record struct GenomeChangedEvent(EntityUid Subject);
