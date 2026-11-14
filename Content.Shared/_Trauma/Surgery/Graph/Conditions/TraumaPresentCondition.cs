using Content.Shared._Trauma.Common.Traumas;
using Content.Shared._Trauma.Traumas;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable]
public sealed partial class TraumaPresentCondition : SurgeryStepCondition
{
    [DataField("trauma")]
    public TraumaType TraumaType = TraumaType.BoneDamage;

    public override bool Check(EntityUid target, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        return entityManager.System<TraumaSystem>().HasWoundableTrauma(target, TraumaType);
    }
}