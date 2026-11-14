using Content.Shared.Body;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph;

[Serializable, NetSerializable]
public sealed partial class SurgeryGraphStepDoAfterEvent : SimpleDoAfterEvent
{
    public readonly ProtoId<SurgeryProcedurePrototype> Procedure;
    public readonly ProtoId<OrganCategoryPrototype>? Organ;
    public readonly int StepIndex;

    public SurgeryGraphStepDoAfterEvent(ProtoId<SurgeryProcedurePrototype> procedure, ProtoId<OrganCategoryPrototype>? organ, int stepIndex)
    {
        Procedure = procedure;
        Organ = organ;
        StepIndex = stepIndex;
    }
}