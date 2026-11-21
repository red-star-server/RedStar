using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class BleedsTreatmentEffect : SurgeryStepEffect
{
    [DataField]
    public FixedPoint2 Amount = 5;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        var body = GetPatient(target, entityManager);
        var woundSystem = entityManager.System<WoundSystem>();
        var healed = woundSystem.TryHealMostSevereBleedingWoundables(body, (float)Amount, out _);

        if (!healed)
            return true;

        if (!entityManager.HasComponent<BloodstreamComponent>(body))
            return true;

        var update = new BloodstreamUpdateEvent();
        entityManager.EventBus.RaiseLocalEvent(body, ref update);

        return true;
    }
}
