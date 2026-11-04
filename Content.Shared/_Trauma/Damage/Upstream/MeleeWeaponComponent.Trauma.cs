namespace Content.Shared.Weapons.Melee;

public sealed partial class MeleeWeaponComponent
{
    /// <summary>
    /// Body-part damage multiplier for targeted melee attacks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ClickPartDamageMultiplier = 1f;

    /// <summary>
    /// Body-part damage multiplier for heavy melee attacks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeavyPartDamageMultiplier = 1f;

    /// <summary>
    /// Wound severity multiplier for heavy melee attacks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeavyAttackWoundMultiplier = 0.5f;
}
