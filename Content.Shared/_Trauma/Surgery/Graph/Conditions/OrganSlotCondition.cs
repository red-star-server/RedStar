using Content.Shared._Trauma.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable, NetSerializable]
public sealed partial class OrganSlotCondition : SurgeryStepCondition
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganSlot;

    public override bool Check(EntityUid target, IEntityManager entityManager)
    {
        return entityManager.System<BodyPartSystem>().HasOrganSlot(target, OrganSlot);
    }
}
