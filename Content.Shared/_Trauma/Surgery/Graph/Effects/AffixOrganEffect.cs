using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Surgery.Steps.Parts;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class AffixOrganEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        if (entityManager.System<BodyPartSystem>().GetOrgan(target, Organ) is { } organ)
            entityManager.RemoveComponent<OrganReattachedComponent>(organ);

        return true;
    }
}
