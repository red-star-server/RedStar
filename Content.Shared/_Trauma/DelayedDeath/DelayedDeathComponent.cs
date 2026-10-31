using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Trauma.DelayedDeath;

/// Kills an entity after a delay when vital organs are missing.
/// Added by <see cref="VitalOrganSystem"/> and <see cref="DebrainedSystem"/>.
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class DelayedDeathComponent : Component
{
    /// <summary>
    /// How long it takes to kill the entity.
    /// </summary>
    [DataField]
    public TimeSpan DeathDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// If true, will prevent *almost* all types of revival.
    /// Right now, this just means it won't allow devils to revive.
    /// </summary>
    [DataField]
    public bool PreventAllRevives;

    /// <summary>
    /// How long it has been since the delayed death timer started.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextDeath;

    /// <summary>
    /// What message is displayed when the time runs out - Goobstation
    /// </summary>
    [DataField]
    public LocId DeathMessageId;

    /// <summary>
    /// What the defib displays when attempting to revive this entity. - Goobstation
    /// </summary>
    [DataField]
    public LocId DefibFailMessageId = "defibrillator-missing-organs";
}
