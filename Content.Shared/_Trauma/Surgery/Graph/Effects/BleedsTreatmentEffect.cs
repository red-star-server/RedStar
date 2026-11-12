using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Traumas;
using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class BleedsTreatmentEffect : SurgeryStepEffect
{
    [DataField]
    public FixedPoint2 Amount = 5;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var healAmount = Amount;
        var changed = false;
        foreach (var wound in entityManager.System<WoundSystem>().GetWoundableWounds(target))
        {
            if (!entityManager.TryGetComponent<BleedInflicterComponent>(wound, out var bleeds) || !bleeds.IsBleeding)
                continue;

            bleeds.ScalingFinishesAt = TimeSpan.Zero;
            if (bleeds.Scaling > healAmount)
            {
                bleeds.Scaling -= healAmount;
                entityManager.Dirty(wound, bleeds);
                changed = true;
                break;
            }

            healAmount -= bleeds.Scaling;
            bleeds.BleedingAmountRaw = 0;
            bleeds.Scaling = 0;
            bleeds.IsBleeding = false;
            entityManager.Dirty(wound, bleeds);
            changed = true;
        }

        if (!changed)
            return true;

        // Keep the bloodstream's bloodloss calculation in sync with the wound state we just changed.
        var body = entityManager.System<BodySystem>().GetBody(target) ?? target;
        if (entityManager.HasComponent<BloodstreamComponent>(body))
        {
            var update = new BloodstreamUpdateEvent();
            entityManager.EventBus.RaiseLocalEvent(body, ref update);
        }

        return true;
    }
}