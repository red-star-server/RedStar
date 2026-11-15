using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable]
public sealed partial class OrganExposedCondition : SurgeryStepCondition
{
    public override bool Check(EntityUid target, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        if (organ is not { } category)
            return false;

        var proto = IoCManager.Resolve<IPrototypeManager>();
        if (!proto.TryIndex(category, out var categoryProto))
            return false;

        var depth = 0;
        if (entityManager.TryGetComponent<OperatedComponent>(target, out var operated)
            && operated.Stage is { } stage
            && proto.TryIndex(stage, out var stageProto))
        {
            depth = stageProto.Depth;
        }

        return depth >= categoryProto.SurgeryDepth;
    }
}
