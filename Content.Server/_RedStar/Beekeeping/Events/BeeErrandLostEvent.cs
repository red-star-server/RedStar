using Content.Server._RedStar.Beekeeping.Data;

namespace Content.Server._RedStar.Beekeeping.Events;

/// <summary>
/// Raised (broadcast) when a bee on an errand is gone without coming home: killed, lost in the pipes or never found its
/// way. The colony already lost the bee; this is for what else should follow (a meat colony's rage).
/// </summary>
[ByRefEvent]
public readonly record struct BeeErrandLostEvent(EntityUid Bee, EntityUid Hive, BeeErrandKind Kind);
