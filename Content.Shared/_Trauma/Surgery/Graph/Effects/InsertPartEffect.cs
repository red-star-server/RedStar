using Content.Shared._Trauma.Surgery.Steps.Parts;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class InsertPartEffect : SurgeryStepEffect
{
    [DataField]
    public ProtoId<OrganCategoryPrototype>? Category;

    public override void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        if (usedItem is not { } item)
            return;

        var body = entityManager.System<BodySystem>();
        if (body.GetCategory(item) is not { } category)
            return;

        if (Category is { } required && category != required)
            return;

        var patient = body.GetBody(target) ?? target;
        if (body.InsertOrgan(patient, item))
            entityManager.EnsureComponent<OrganReattachedComponent>(item);
    }
}
