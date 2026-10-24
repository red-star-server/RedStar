namespace Content.Shared.Weapons.Melee;

public sealed partial class MeleeWeaponComponent
{
    [DataField, AutoNetworkedField]
    public float ClickPartDamageMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public float HeavyPartDamageMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public float HeavyAttackWoundMultiplier = 0.5f;
}
