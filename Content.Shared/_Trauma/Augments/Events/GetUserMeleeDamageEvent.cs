using Content.Shared.Damage;

namespace Content.Shared._Trauma.Augments;

/// <summary>
/// Raised on the user for damage bonuses applied to punches and held melee weapons.
/// </summary>
[ByRefEvent]
public record struct GetUserMeleeDamageEvent(EntityUid Weapon, DamageSpecifier Damage, List<DamageModifierSet> Modifiers);
