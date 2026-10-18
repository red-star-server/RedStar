using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Xenobiology.Components.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SampleCollectorComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(4f);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier? Damage;

    [DataField, AutoNetworkedField]
    public float MovementThreshold = 0.5f;

    /// <summary>
    /// Biopsy is rejected when the subject's positive cellular damage reaches this value.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaximumCellularDamage = 120f;

    /// <summary>
    /// Damage amount corresponding to one full CellularDamagePenalty in sample stability.
    /// </summary>
    [DataField]
    public float CellularDamageScale = 100f;

    /// <summary>
    /// Fraction of generated stability lost per CellularDamageScale damage, before final clamping.
    /// </summary>
    [DataField]
    public float CellularDamagePenalty = 0.5f;

    /// <summary>
    /// Lower bound before the cellular damage penalty is applied; final stability may be lower.
    /// </summary>
    [DataField]
    public float MinimumStability = 0.1f;

    /// <summary>
    /// Stability subtracted per gene before applying the cellular damage penalty.
    /// </summary>
    [DataField]
    public float GeneStabilityCost = 0.05f;

    /// <summary>
    /// Maximum random deviation added to stability before the cellular damage penalty.
    /// </summary>
    [DataField]
    public float StabilityVariation = 0.03f;
}
