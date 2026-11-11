using Content.Shared._Trauma.Common.Traumas;
using Content.Shared._Trauma.Traumas;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable, NetSerializable]
public sealed partial class TraumaPresentCondition : SurgeryStepCondition
{
    [DataField("trauma")]
    public TraumaType TraumaType = TraumaType.BoneDamage;

    public override bool Check(EntityUid target, IEntityManager entityManager)
    {
        return entityManager.System<TraumaSystem>().HasWoundableTrauma(target, TraumaType);
    }
}