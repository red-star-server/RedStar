using Content.Shared._Trauma.Body;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class RemoveOrganEffect : SurgeryStepEffect
{
    /// <summary>
    /// Fixed category; when unset, the procedure instance's organ is used.
    /// </summary>
    [DataField]
    public ProtoId<OrganCategoryPrototype>? Organ;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        if ((Organ ?? organ) is not { } category)
            return false;

        var part = entityManager.System<BodyPartSystem>();
        if (part.GetOrgan(target, category) is not { } organUid || !part.RemoveOrgan(target, organUid))
            return false;

        if (surgeon is { } user)
            entityManager.System<SharedHandsSystem>().TryPickupAnyHand(user, organUid);

        return true;
    }
}