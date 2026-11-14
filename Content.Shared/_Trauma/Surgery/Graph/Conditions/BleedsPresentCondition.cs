using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable]
public sealed partial class BleedsPresentCondition : SurgeryStepCondition
{
    public override bool Check(EntityUid target, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        return entityManager.TryGetComponent<WoundableComponent>(target, out var woundable) && woundable.Bleeds > 0;
    }
}