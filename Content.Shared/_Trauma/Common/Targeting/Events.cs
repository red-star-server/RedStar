namespace Content.Shared._Trauma.Common.Targeting;

/// <summary>
/// Raised on a mob to try get the bodypart of a target entity it's targeting.
/// </summary>
[ByRefEvent]
public record struct GetTargetedPartEvent(EntityUid Target, EntityUid? Part = null);
