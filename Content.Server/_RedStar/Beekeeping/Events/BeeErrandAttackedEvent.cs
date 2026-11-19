namespace Content.Server._RedStar.Beekeeping.Events;

/// <summary>
/// Raised (broadcast) when someone hurts a forager or the scout of a hive: the colony may defend it.
/// </summary>
[ByRefEvent]
public readonly record struct BeeErrandAttackedEvent(EntityUid Bee, EntityUid Hive, EntityUid Attacker);
