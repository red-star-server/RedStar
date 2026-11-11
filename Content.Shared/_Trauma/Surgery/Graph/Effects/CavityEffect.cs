using Content.Shared._Trauma.Common.Body;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable, NetSerializable]
public sealed partial class CavityEffect : SurgeryStepEffect
{
    [DataField]
    public bool Remove;

    public override void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        if (surgeon is not { } user)
            return;

        var ev = new GetBodyPartCavityEvent();
        entityManager.EventBus.RaiseLocalEvent(target, ref ev);
        if (ev.Container is not { } container)
            return;

        var hands = entityManager.System<SharedHandsSystem>();
        if (Remove)
        {
            if (container.ContainedEntity is { } contained)
                hands.TryPickupAnyHand(user, contained);
        }
        else
        {
            var item = usedItem ?? hands.GetActiveItem(user);
            if (item is { } held)
                entityManager.System<SharedContainerSystem>().Insert(held, container);
        }
    }
}
