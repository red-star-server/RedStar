using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable]
public sealed partial class CavityItemPresentCondition : SurgeryStepCondition
{
    public override bool Check(EntityUid target, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        var ev = new GetBodyPartCavityEvent();
        entityManager.EventBus.RaiseLocalEvent(target, ref ev);
        return ev.Container?.ContainedEntity != null;
    }
}
