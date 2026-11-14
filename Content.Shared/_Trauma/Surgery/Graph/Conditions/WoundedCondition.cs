using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

/// <summary>
/// The part has healable wounds of the given damage group.
/// </summary>
[Serializable]
public sealed partial class WoundedCondition : SurgeryStepCondition
{
    [DataField]
    public ProtoId<DamageGroupPrototype> DamageGroup = "Brute";

    public override bool Check(EntityUid target, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        return entityManager.System<WoundSystem>().GetWoundableSeverityPoint(target, DamageGroup, healable: true) > 0;
    }
}