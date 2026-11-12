using Content.Shared._Trauma.Traumas;
using Content.Shared._Trauma.Wounds;
using Content.Shared.FixedPoint;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class BleedsTreatmentEffect : SurgeryStepEffect
{
    [DataField]
    public FixedPoint2 Amount = 5;

    public override void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var healAmount = Amount;
        foreach (var wound in entityManager.System<WoundSystem>().GetWoundableWounds(target))
        {
            if (!entityManager.TryGetComponent<BleedInflicterComponent>(wound, out var bleeds) || !bleeds.IsBleeding)
                continue;

            bleeds.ScalingFinishesAt = TimeSpan.Zero;
            if (bleeds.Scaling > healAmount)
            {
                bleeds.Scaling -= healAmount;
                entityManager.Dirty(wound, bleeds);
                break;
            }

            healAmount -= bleeds.Scaling;
            bleeds.BleedingAmountRaw = 0;
            bleeds.Scaling = 0;
            bleeds.IsBleeding = false;
            entityManager.Dirty(wound, bleeds);
        }
    }
}
