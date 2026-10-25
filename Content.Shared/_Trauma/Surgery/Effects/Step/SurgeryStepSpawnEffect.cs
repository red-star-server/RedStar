using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;


namespace Content.Shared._Trauma.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryStepSpawnEffectComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Entity;
}
