namespace Content.Shared._Trauma.Surgery.Conditions;

/// <summary>
///     Raised on the entity that is receiving surgery.
/// </summary>
[ByRefEvent]
public record struct SurgeryValidEvent(EntityUid Body, EntityUid Part, EntityUid User, bool Cancelled = false, string? Reason = null);
