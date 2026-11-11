using Content.Shared._Trauma.Wounds;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable, NetSerializable]
public sealed partial class WoundedCondition : SurgeryStepCondition
{
    [DataField]
    public ProtoId<DamageGroupPrototype> DamageGroup = "Brute";

    public override bool Check(EntityUid target, IEntityManager entityManager)
    {
        return entityManager.System<WoundSystem>().GetWoundableSeverityPoint(target, DamageGroup, healable: true) > 0;
    }
}
