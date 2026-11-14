using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class CavityEffect : SurgeryStepEffect
{
    [DataField]
    public bool Remove;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        if (surgeon is not { } user)
            return true;

        var ev = new GetBodyPartCavityEvent();
        entityManager.EventBus.RaiseLocalEvent(target, ref ev);
        if (ev.Container is not { } container)
            return true;

        var hands = entityManager.System<SharedHandsSystem>();
        if (Remove)
        {
            return container.ContainedEntity is not { } contained || hands.TryPickupAnyHand(user, contained);
        }

        var item = usedItem ?? hands.GetActiveItem(user);
        return item is not { } held || entityManager.System<SharedContainerSystem>().Insert(held, container);
    }
}
