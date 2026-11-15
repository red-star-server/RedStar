using Content.Shared.FixedPoint;

namespace Content.Shared._Trauma.Common.Damage;

/// <summary>
/// Event raised on a damageable entity after its damage has been directly set, e.g. cleared.
/// </summary>
[ByRefEvent]
public readonly record struct DamageSetEvent(FixedPoint2 Damage);
