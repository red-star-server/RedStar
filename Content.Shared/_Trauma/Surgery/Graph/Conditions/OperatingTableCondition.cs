using Content.Shared.Body;
using Content.Shared.Buckle.Components;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable]
public sealed partial class OperatingTableCondition : SurgeryStepCondition
{
    public override bool Check(EntityUid target, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>().GetBody(target) ?? target;
        return entityManager.TryGetComponent<BuckleComponent>(body, out var buckle)
            && buckle.BuckledTo is { } table
            && entityManager.HasComponent<OperatingTableComponent>(table);
    }
}
