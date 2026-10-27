namespace Content.Shared.Damage.Components;

public sealed partial class DamageOtherOnHitComponent
{
    /// <summary>
    /// Whether hits may only increase the target's damage.
    /// </summary>
    [DataField]
    public bool IncreaseOnly;
}
