using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Surgery.Steps.Parts;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable]
public sealed partial class PartRemovedCondition : SurgeryStepCondition
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;

    public override bool Check(EntityUid target, IEntityManager entityManager)
    {
        var part = entityManager.System<BodyPartSystem>();
        if (part.GetOrgan(target, Category) is { } attached)
            return entityManager.HasComponent<OrganReattachedComponent>(attached);

        return part.CanInsertOrgan(target, Category);
    }
}
