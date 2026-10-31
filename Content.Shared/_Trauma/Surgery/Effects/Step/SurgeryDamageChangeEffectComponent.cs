using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryDamageChangeEffectComponent : Component
{
    /// <summary>
    /// Damage change applied to the selected body part after the step.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// Multiplier applied to the damage change while the patient is asleep.
    /// </summary>
    [DataField]
    public float SleepModifier = 0.5f;

    /// <summary>
    /// Consumable flag retained for compatibility with surgery prototype data.
    /// </summary>
    [DataField]
    public bool IsConsumable;
}
