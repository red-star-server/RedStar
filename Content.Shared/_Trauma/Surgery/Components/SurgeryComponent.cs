using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery;

/// <summary>
/// Component for surgery entity prototypes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[EntityCategory("Surgeries")]
public sealed partial class SurgeryComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Priority;

    [DataField, AutoNetworkedField]
    public EntProtoId? Requirement;

    [DataField(required: true), AutoNetworkedField]
    public List<EntProtoId> Steps = new();
}
