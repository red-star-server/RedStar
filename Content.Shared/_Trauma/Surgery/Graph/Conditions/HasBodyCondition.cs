using Content.Shared.Body;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable, NetSerializable]
public sealed partial class HasBodyCondition : SurgeryStepCondition
{
    public override bool Check(EntityUid target, IEntityManager entityManager)
    {
        return entityManager.System<BodySystem>().GetBody(target) != null;
    }
}
