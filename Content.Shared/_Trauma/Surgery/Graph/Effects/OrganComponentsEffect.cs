using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable, NetSerializable]
public sealed partial class OrganComponentsEffect : SurgeryStepEffect
{
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, ComponentRegistry>? Add;

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, ComponentRegistry>? Remove;

    public override void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>().GetBody(target) ?? target;
        var organs = entityManager.System<BodyPartSystem>().GetPartOrgans(target);

        if (Add is { } add)
            Modify(organs, body, add, entityManager, false);
        if (Remove is { } remove)
            Modify(organs, body, remove, entityManager, true);
    }

    private static void Modify(
        Dictionary<ProtoId<OrganCategoryPrototype>, Entity<OrganComponent>> organs,
        EntityUid body,
        Dictionary<ProtoId<OrganCategoryPrototype>, ComponentRegistry> modifications,
        IEntityManager entityManager,
        bool remove)
    {
        foreach (var (category, components) in modifications)
        {
            if (!organs.TryGetValue(category, out var organ))
                continue;

            var comp = entityManager.EnsureComponent<OrganComponentsComponent>(organ);
            if (remove)
            {
                entityManager.RemoveComponents(body, components);
                foreach (var key in components.Keys)
                {
                    comp.OnAdd?.Remove(key);
                    comp.AddedKeys.Remove(key);
                }
            }
            else
            {
                comp.OnAdd ??= new ComponentRegistry();
                foreach (var (key, entry) in components)
                {
                    comp.OnAdd[key] = entry;
                    comp.AddedKeys.Add(key);
                }

                entityManager.AddComponents(body, components);
            }

            entityManager.Dirty(organ, comp);
        }
    }
}
