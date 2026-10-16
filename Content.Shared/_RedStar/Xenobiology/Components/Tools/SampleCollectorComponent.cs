using Content.Shared.Damage;

namespace Content.Shared._RedStar.Xenobiology.Components.Tools;

[RegisterComponent]
public sealed partial class SampleCollectorComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Delay = TimeSpan.FromSeconds(4f);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier? Damage;

    [DataField]
    public float MovementThreshold = 0.5f;

    [DataField]
    public float MaximumCellularDamage = 120f;

    [DataField]
    public float CellularDamageScale = 100f;

    [DataField]
    public float CellularDamagePenalty = 0.5f;

    [DataField]
    public float MinimumStability = 0.1f;

    [DataField]
    public float GeneStabilityCost = 0.05f;

    [DataField]
    public float StabilityVariation = 0.03f;
}
