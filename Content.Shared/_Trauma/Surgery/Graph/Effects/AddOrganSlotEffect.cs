using Content.Shared._Trauma.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class AddOrganSlotEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganSlot;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        entityManager.System<BodyPartSystem>().TryAddSlot(target, OrganSlot);
        return true;
    }
}
