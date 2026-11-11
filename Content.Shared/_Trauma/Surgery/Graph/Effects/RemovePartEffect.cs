using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable, NetSerializable]
public sealed partial class RemovePartEffect : SurgeryStepEffect
{
    public override void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>();
        var patient = body.GetBody(target) ?? target;
        if (!body.RemoveOrgan(patient, target))
            return;

        if (surgeon is { } user)
            entityManager.System<SharedHandsSystem>().TryPickupAnyHand(user, target);
    }
}
