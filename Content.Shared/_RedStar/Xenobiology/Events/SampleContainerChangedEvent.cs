namespace Content.Shared._RedStar.Xenobiology.Events;

/// <summary>
/// Raised after the contents of a sample container change.
/// </summary>
[ByRefEvent]
public readonly record struct SampleContainerChangedEvent;

/// <summary>
/// Relayed to the entity holding a sample container after its contents change.
/// </summary>
[ByRefEvent]
public readonly record struct ContainedSamplesChangedEvent(EntityUid SampleContainer);
