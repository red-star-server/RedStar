using Content.Shared._Trauma.Body;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable, NetSerializable]
public sealed partial class PartPresentCondition : SurgeryStepCondition
{
    public override bool Check(EntityUid target, IEntityManager entityManager)
    {
        return entityManager.HasComponent<BodyPartComponent>(target);
    }
}
