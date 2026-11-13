using Content.Shared._Trauma.Common.Wounds;
using Content.Shared.FixedPoint;

namespace Content.Shared._Trauma.Wounds;

[ByRefEvent]
public record struct WoundAddedEvent(WoundComponent Component, WoundableComponent Woundable);

[ByRefEvent]
public record struct WoundSeverityPointChangedEvent(WoundComponent Component, FixedPoint2 OldSeverity, FixedPoint2 NewSeverity, FixedPoint2 RequestedChange);

[ByRefEvent]
public record struct WoundSeverityChangedEvent(WoundSeverity OldSeverity, WoundSeverity NewSeverity);

[ByRefEvent]
public record struct WoundHealAttemptEvent(Entity<WoundableComponent> Woundable, bool IgnoreBlockers = false, bool Cancelled = false);

[ByRefEvent]
public record struct WoundHealAttemptOnWoundableEvent(EntityUid Wound, bool Cancelled = false);
