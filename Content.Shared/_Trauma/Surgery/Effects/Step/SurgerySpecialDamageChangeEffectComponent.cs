using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgerySpecialDamageChangeEffectComponent : Component
{
    /// <summary>
    /// Damage-type identifier retained by the special surgery effect data.
    /// </summary>
    [DataField]
    public string DamageType = "";

    /// <summary>
    /// Consumable flag retained for compatibility with surgery prototype data.
    /// </summary>
    [DataField]
    public bool IsConsumable;
}
