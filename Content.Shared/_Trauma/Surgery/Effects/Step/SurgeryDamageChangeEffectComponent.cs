using Robust.Shared.GameStates;

using Content.Shared.Damage;

namespace Content.Shared._Trauma.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryDamageChangeEffectComponent : Component
{
    [DataField]
    public DamageSpecifier Damage = default!;

    [DataField]
    public float SleepModifier = 0.5f;

    [DataField]
    public bool IsConsumable;
}
