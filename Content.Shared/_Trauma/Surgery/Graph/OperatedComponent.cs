using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OperatedComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<SurgeryGraphPrototype>? GraphId;

    [DataField, AutoNetworkedField]
    public string CurrentNode = string.Empty;

    [DataField, AutoNetworkedField]
    public string? ActiveEdgeId;

    [DataField, AutoNetworkedField]
    public int CompletedSteps;

    [ViewVariables]
    public bool IsOperating;

    [ViewVariables]
    public DoAfterId? DoAfter;
}
