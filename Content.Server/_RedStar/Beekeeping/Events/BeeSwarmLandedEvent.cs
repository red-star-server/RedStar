namespace Content.Server._RedStar.Beekeeping.Events;

/// <summary>
/// Raised on a swarm that has just built its wild hive, right before the swarm is gone.
/// </summary>
[ByRefEvent]
public readonly record struct BeeSwarmLandedEvent(EntityUid Hive);
