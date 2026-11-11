using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Surgery.Steps.Parts;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable, NetSerializable]
public sealed partial class AffixOrganEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    public override void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        if (entityManager.System<BodyPartSystem>().GetOrgan(target, Organ) is { } organ)
            entityManager.RemoveComponent<OrganReattachedComponent>(organ);
    }
}
