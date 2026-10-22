using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RedStar.Genetics.Components;

/// <summary>
/// Configures overload symptoms and schedules damage while acquired genes exceed safe capacity.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class GeneticOverloadComponent : Component
{
    [DataField]
    public TimeSpan PulseInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Status maintained while overloaded. Its modifiers combine with other movement effects normally.
    /// </summary>
    [DataField]
    public EntProtoId Weakness = "GeneticOverloadStatusEffect";

    /// <summary>
    /// Damage per pulse at SevereDamageThreshold. Scaled down for mild overload and up for severe overload.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new() { DamageDict = new() { ["Cellular"] = 1 } };

    /// <summary>
    /// Internal overload damage ignores armor and species resistances by default.
    /// </summary>
    [DataField]
    public bool IgnoreResistances = true;

    /// <summary>
    /// Whether a damaging pulse interrupts the patient's ongoing timed actions.
    /// </summary>
    [DataField]
    public bool InterruptsDoAfters;

    /// <summary>
    /// Load percentage above which damage starts. Overload below this threshold causes only weakness.
    /// </summary>
    [DataField]
    public float DamageThreshold = 110;

    /// <summary>
    /// Load percentage at which a full damage pulse is reached and quadratic scaling begins.
    /// </summary>
    [DataField]
    public float SevereDamageThreshold = 125;

    /// <summary>
    /// Additional damage multiplier per squared percentage point above SevereDamageThreshold.
    /// </summary>
    [DataField]
    public float SevereDamageScale = 0.03125f;

    /// <summary>
    /// Whether symptoms are active. Configuration is retained after the organism returns to safe load.
    /// </summary>
    [DataField]
    public bool Active;

    /// <summary>
    /// Next damage pulse. Changing genes without ending overload does not restart the interval.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPulse;
}
