using Content.Shared.Body;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph;

/// <summary>
/// Surgery graph state of a body part: its current stage and the procedure in progress.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OperatedComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<SurgeryStagePrototype>? Stage;

    [DataField, AutoNetworkedField]
    public ProtoId<SurgeryProcedurePrototype>? ActiveProcedure;

    [DataField, AutoNetworkedField]
    public ProtoId<OrganCategoryPrototype>? ActiveOrgan;

    [DataField, AutoNetworkedField]
    public int CompletedSteps;

    [ViewVariables]
    public bool IsOperating;

    [ViewVariables]
    public DoAfterId? DoAfter;
}