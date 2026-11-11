using Content.Shared._Trauma.Body;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable, NetSerializable]
public sealed partial class RemoveOrganEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    public override void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var part = entityManager.System<BodyPartSystem>();
        if (part.GetOrgan(target, Organ) is not { } organ || !part.RemoveOrgan(target, organ))
            return;

        if (surgeon is { } user)
            entityManager.System<SharedHandsSystem>().TryPickupAnyHand(user, organ);
    }
}
