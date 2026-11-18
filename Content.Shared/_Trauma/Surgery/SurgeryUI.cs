using Content.Shared._Trauma.Surgery.Graph;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery;

[Serializable, NetSerializable]
public enum SurgeryUIKey { Key }

[Serializable, NetSerializable]
public sealed class SurgeryStepChosenBuiMsg(
    NetEntity part,
    ProtoId<SurgeryProcedurePrototype> procedure,
    int stepIndex,
    ProtoId<OrganCategoryPrototype>? organ = null) : BoundUserInterfaceMessage
{
    public readonly NetEntity Part = part;
    public readonly ProtoId<SurgeryProcedurePrototype> Procedure = procedure;
    public readonly int StepIndex = stepIndex;
    public readonly ProtoId<OrganCategoryPrototype>? Organ = organ;
}
