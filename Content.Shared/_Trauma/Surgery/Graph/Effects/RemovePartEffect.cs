using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class RemovePartEffect : SurgeryStepEffect
{
    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>();
        var patient = body.GetBody(target) ?? target;
        if (!body.RemoveOrgan(patient, target))
            return false;

        if (surgeon is { } user)
            entityManager.System<SharedHandsSystem>().TryPickupAnyHand(user, target);

        return true;
    }
}