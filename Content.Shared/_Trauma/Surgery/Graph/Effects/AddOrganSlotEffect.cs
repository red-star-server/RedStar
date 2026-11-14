using Content.Shared._Trauma.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class AddOrganSlotEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganSlot = default!;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        entityManager.System<BodyPartSystem>().TryAddSlot(target, OrganSlot);
        return true;
    }
}