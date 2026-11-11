using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Surgery.Steps.Parts;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable, NetSerializable]
public sealed partial class InsertOrganEffect : SurgeryStepEffect
{
    [DataField]
    public ProtoId<OrganCategoryPrototype>? Organ;

    public override void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        if (usedItem is not { } item || entityManager.System<BodySystem>().GetCategory(item) is not { } category)
            return;

        if (Organ is { } required && category != required)
            return;

        if (entityManager.System<BodyPartSystem>().InsertOrgan(target, item))
            entityManager.EnsureComponent<OrganReattachedComponent>(item);
    }
}
