using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class ComponentEffect : SurgeryStepEffect
{
    [DataField]
    public ComponentRegistry? Add;

    [DataField]
    public ComponentRegistry? Remove;

    [DataField]
    public ComponentRegistry? BodyAdd;

    [DataField]
    public ComponentRegistry? BodyRemove;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        if (Add is { } add)
            entityManager.AddComponents(target, add);
        if (Remove is { } remove)
            entityManager.RemoveComponents(target, remove);

        if (BodyAdd is null && BodyRemove is null)
            return true;

        var body = entityManager.System<BodySystem>().GetBody(target) ?? target;
        if (BodyAdd is { } bodyAdd)
            entityManager.AddComponents(body, bodyAdd);
        if (BodyRemove is { } bodyRemove)
            entityManager.RemoveComponents(body, bodyRemove);

        return true;
    }
}