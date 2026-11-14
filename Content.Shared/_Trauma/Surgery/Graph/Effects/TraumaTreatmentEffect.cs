using System.Linq;
using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Traumas;
using Content.Shared._Trauma.Traumas;
using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class TraumaTreatmentEffect : SurgeryStepEffect
{
    [DataField]
    public TraumaType TraumaType = TraumaType.BoneDamage;

    [DataField]
    public FixedPoint2 Amount = 5;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        var trauma = entityManager.System<TraumaSystem>();
        var healAmount = Amount;
        switch (TraumaType)
        {
            case TraumaType.OrganDamage:
                foreach (var organUid in entityManager.System<BodyPartSystem>().GetPartOrgans(target).Values.ToArray())
                {
                    if (!entityManager.TryGetComponent<InternalChildOrganComponent>(organUid, out var childOrgan))
                        continue;

                    foreach (var modifier in childOrgan.IntegrityModifiers.ToArray())
                    {
                        // Only heal wound-based organ damage; leave other integrity modifiers (e.g. eye damage) alone.
                        if (modifier.Key.Item1 != TraumaSystem.WoundableDamageModifier)
                            continue;

                        if (healAmount <= 0)
                            return true;

                        if (modifier.Value <= healAmount)
                        {
                            healAmount -= modifier.Value;
                            trauma.TryRemoveOrganDamageModifier((organUid, childOrgan), modifier.Key.Item2, modifier.Key.Item1);
                        }
                        else
                        {
                            trauma.TryChangeOrganDamageModifier((organUid, childOrgan), -healAmount, modifier.Key.Item2, modifier.Key.Item1);
                            return true;
                        }
                    }
                }

                break;

            case TraumaType.BoneDamage:
                trauma.DamageBone(target, -healAmount);
                break;

            case TraumaType.Dismemberment:
                trauma.RemoveTraumas(target, TraumaType.Dismemberment);
                break;
        }

        return true;
    }
}