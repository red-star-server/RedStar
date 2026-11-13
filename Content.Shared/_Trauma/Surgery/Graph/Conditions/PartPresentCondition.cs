using Content.Shared._Trauma.Body;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable]
public sealed partial class PartPresentCondition : SurgeryStepCondition
{
    public override bool Check(EntityUid target, IEntityManager entityManager)
    {
        return entityManager.HasComponent<BodyPartComponent>(target);
    }
}
