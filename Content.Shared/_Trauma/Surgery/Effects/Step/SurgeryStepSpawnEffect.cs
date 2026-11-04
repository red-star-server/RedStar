using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryStepSpawnEffectComponent : Component
{
    /// <summary>
    /// Entity prototype spawned at the patient when the step completes.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Entity;
}
