using Content.Shared._Trauma.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class AffixOrganEffect : SurgeryStepEffect
{
    [DataField]
    public ProtoId<OrganCategoryPrototype>? Organ;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        if ((Organ ?? organ) is not { } category)
            return false;

        if (entityManager.System<BodyPartSystem>().GetOrgan(target, category) is not { } organUid)
            return false;

        entityManager.RemoveComponent<OrganReattachedComponent>(organUid);
        return true;
    }
}
