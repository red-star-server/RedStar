using Content.Shared.Damage;

namespace Content.Shared._Trauma.Common.Damage;

/// <summary>
/// Raised on an entity taking damage from the HealthChangeEntityEffect system.
/// </summary>
[ByRefEvent]
public record struct OnHealthChangeEvent(DamageSpecifier Damage);
