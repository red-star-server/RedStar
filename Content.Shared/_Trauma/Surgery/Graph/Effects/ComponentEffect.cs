using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable, NetSerializable]
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

    public override void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        if (Add is { } add)
            entityManager.AddComponents(target, add);
        if (Remove is { } remove)
            entityManager.RemoveComponents(target, remove);

        if (BodyAdd is null && BodyRemove is null)
            return;

        var body = entityManager.System<BodySystem>().GetBody(target) ?? target;
        if (BodyAdd is { } bodyAdd)
            entityManager.AddComponents(body, bodyAdd);
        if (BodyRemove is { } bodyRemove)
            entityManager.RemoveComponents(body, bodyRemove);
    }
}
