namespace Content.Server._RedStar.Beekeeping.Events;

/// <summary>
/// Raised (broadcast) when a broken wild hive dropped its combs, so a colony that kept something else in them can add it.
/// </summary>
[ByRefEvent]
public readonly record struct WildCombsDroppedEvent(EntityUid Hive, EntityUid Combs, int Count);
