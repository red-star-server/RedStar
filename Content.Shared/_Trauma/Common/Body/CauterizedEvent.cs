using Content.Shared.FixedPoint;

namespace Content.Shared._Trauma.Common.Body;

/// <summary>
/// Event raised on a mob after bleeding is reduced.
/// Amount is a positive number of bleed stacks.
/// </summary>
[ByRefEvent]
public record struct CauterizedEvent(FixedPoint2 Amount);
