namespace Content.Server._RedStar.Beekeeping.Events;

/// <summary>
/// Raised on a bee on an errand when it goes into its hive, just before it is removed: the colony gets it back.
/// </summary>
[ByRefEvent]
public readonly record struct BeeErrandHomeEvent(EntityUid Hive);
