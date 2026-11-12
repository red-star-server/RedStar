using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class OrganComponentsEffect : SurgeryStepEffect
{
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, ComponentRegistry>? Add;

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, ComponentRegistry>? Remove;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>().GetBody(target) ?? target;
        var organs = entityManager.System<BodyPartSystem>().GetPartOrgans(target);
        var system = entityManager.System<OrganComponentsSystem>();

        if (Add is { } add)
            Modify(organs, body, add, system, entityManager, false);
        if (Remove is { } remove)
            Modify(organs, body, remove, system, entityManager, true);

        return true;
    }

    private static void Modify(
        Dictionary<ProtoId<OrganCategoryPrototype>, Entity<OrganComponent>> organs,
        EntityUid body,
        Dictionary<ProtoId<OrganCategoryPrototype>, ComponentRegistry> modifications,
        OrganComponentsSystem system,
        IEntityManager entityManager,
        bool remove)
    {
        foreach (var (category, components) in modifications)
        {
            if (!organs.TryGetValue(category, out var organ))
                continue;

            var comp = entityManager.EnsureComponent<OrganComponentsComponent>(organ);
            comp.OnAdd ??= new ComponentRegistry();
            foreach (var (key, entry) in components)
            {
                if (remove)
                    comp.OnAdd.Remove(key);
                else
                    comp.OnAdd[key] = entry;
            }

            entityManager.Dirty(organ, comp);
            // Let the organ system apply/defer the components based on its enabled state.
            system.RefreshOrganComponents(new Entity<OrganComponentsComponent>(organ.Owner, comp), body);
        }
    }
}