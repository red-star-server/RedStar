using Content.Shared.Body;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable]
public sealed partial class HasBodyCondition : SurgeryStepCondition
{
    public override bool Check(EntityUid target, IEntityManager entityManager)
    {
        return entityManager.System<BodySystem>().GetBody(target) != null;
    }
}
