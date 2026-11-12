using Content.Shared._Trauma.Body;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class RemoveOrganEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var part = entityManager.System<BodyPartSystem>();
        if (part.GetOrgan(target, Organ) is not { } organ || !part.RemoveOrgan(target, organ))
            return false;

        if (surgeon is { } user)
            entityManager.System<SharedHandsSystem>().TryPickupAnyHand(user, organ);

        return true;
    }
}
