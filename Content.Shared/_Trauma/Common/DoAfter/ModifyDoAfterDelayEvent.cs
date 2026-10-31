namespace Content.Shared._Trauma.Common.DoAfter;

/// <summary>
/// Raised on the target, relayed to bodyparts and bone entities in hand organ entities.
/// </summary>
[ByRefEvent]
public record struct ModifyDoAfterDelayEvent(float Multiplier = 1f);
