using Content.Shared._Trauma.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable]
public sealed partial class OrganSlotCondition : SurgeryStepCondition
{
    [DataField]
    public ProtoId<OrganCategoryPrototype>? OrganSlot;

    public override bool Check(EntityUid target, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        return (OrganSlot ?? organ) is { } category && entityManager.System<BodyPartSystem>().HasOrganSlot(target, category);
    }
}
