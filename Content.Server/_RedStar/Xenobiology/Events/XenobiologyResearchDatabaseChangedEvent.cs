namespace Content.Server._RedStar.Xenobiology.Events;

/// <summary>
/// Raised on research clients when their server catalogues a new sample type.
/// </summary>
[ByRefEvent]
public readonly record struct XenobiologyResearchDatabaseChangedEvent;
