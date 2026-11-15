using Content.Shared._Trauma.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable]
public sealed partial class PartRemovedCondition : SurgeryStepCondition
{
    [DataField]
    public ProtoId<OrganCategoryPrototype>? Category;

    public override bool Check(EntityUid target, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        if ((Category ?? organ) is not { } category)
            return false;

        var part = entityManager.System<BodyPartSystem>();
        if (part.GetOrgan(target, category) is { } attached)
            return entityManager.HasComponent<OrganReattachedComponent>(attached);

        return part.CanInsertOrgan(target, category);
    }
}
