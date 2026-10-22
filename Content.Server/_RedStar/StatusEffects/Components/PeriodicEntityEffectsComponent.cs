using Content.Server._RedStar.StatusEffects.Systems;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RedStar.StatusEffects.Components;

/// <summary>
/// Runs a conditional effect package on the host of a status effect at fixed intervals.
/// Each status owns its own schedule; removing it stops the package.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(PeriodicEntityEffectsSystem))]
public sealed partial class PeriodicEntityEffectsComponent : Component
{
    /// <summary>
    /// Time between packages. Must be positive; the first package waits for a full interval.
    /// </summary>
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Actions applied to the host when the package is due and its conditions pass.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = [];

    /// <summary>
    /// Conditions checked once for the entire package, before any effects are applied.
    /// Individual effects may additionally supply their own conditions.
    /// </summary>
    [DataField]
    public EntityCondition[]? Conditions;

    /// <summary>
    /// Minimum delay after the host changes position. Zero leaves movement unrestricted.
    /// </summary>
    [DataField]
    public TimeSpan DelayAfterMovement;

    /// <summary>
    /// Minimum delay after the host receives positive damage. Healing does not restart it.
    /// </summary>
    [DataField]
    public TimeSpan DelayAfterDamage;

    /// <summary>
    /// Earliest next package time, shifted with pauses and serialized relative to simulation time.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextEffect;
}
