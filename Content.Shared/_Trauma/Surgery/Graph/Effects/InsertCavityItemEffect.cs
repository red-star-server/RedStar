using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class InsertCavityItemEffect : SurgeryStepEffect
{
    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        var ev = new GetBodyPartCavityEvent();
        entityManager.EventBus.RaiseLocalEvent(target, ref ev);

        return ev.Container is { } container &&
               usedItem is { } held &&
               held != surgeon &&
               entityManager.System<SharedContainerSystem>().Insert(held, container);
    }
}
