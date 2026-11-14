using Content.Shared._Trauma.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class InsertOrganEffect : SurgeryStepEffect
{
    /// <summary>
    /// Fixed category; when unset, the procedure instance's organ is used.
    /// </summary>
    [DataField]
    public ProtoId<OrganCategoryPrototype>? Organ;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        if (usedItem is not { } item || entityManager.System<BodySystem>().GetCategory(item) is not { } category)
            return false;

        if ((Organ ?? organ) is { } required && category != required)
            return false;

        if (!entityManager.System<BodyPartSystem>().InsertOrgan(target, item))
            return false;

        entityManager.EnsureComponent<OrganReattachedComponent>(item);
        return true;
    }
}