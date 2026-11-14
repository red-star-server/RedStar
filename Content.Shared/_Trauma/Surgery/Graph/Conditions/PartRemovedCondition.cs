using Content.Shared._Trauma.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

/// <summary>
/// The organ slot is free (or holds a not-yet-affixed reattachment), so an organ of this category can be inserted.
/// </summary>
[Serializable]
public sealed partial class PartRemovedCondition : SurgeryStepCondition
{
    /// <summary>
    /// Fixed category; when unset, the procedure instance's organ is used.
    /// </summary>
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