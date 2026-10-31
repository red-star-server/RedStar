using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Trauma.Body;

/// <summary>
/// Makes this organ try to run entity effects on its body periodically, with a random delay.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RandomOrganEffectsSystem))]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class RandomOrganEffectsComponent : Component
{
    /// <summary>
    /// List of entity effects to apply to the organ's body.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// What is the minimum time between activations?
    /// </summary>
    [DataField]
    public TimeSpan MinActivationTime = TimeSpan.FromSeconds(60);

    /// <summary>
    /// What is the maximum time between activations?
    /// </summary>
    [DataField]
    public TimeSpan MaxActivationTime = TimeSpan.FromSeconds(300);

    /// <summary>
    /// The next time the organ will activate.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextUpdate;
}
