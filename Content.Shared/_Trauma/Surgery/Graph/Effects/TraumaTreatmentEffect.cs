using System.Linq;
using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Traumas;
using Content.Shared._Trauma.Traumas;
using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable, NetSerializable]
public sealed partial class TraumaTreatmentEffect : SurgeryStepEffect
{
    [DataField]
    public TraumaType TraumaType = TraumaType.BoneDamage;

    [DataField]
    public FixedPoint2 Amount = 5;

    public override void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var trauma = entityManager.System<TraumaSystem>();
        var healAmount = Amount;
        switch (TraumaType)
        {
            case TraumaType.OrganDamage:
                foreach (var organUid in entityManager.System<BodyPartSystem>().GetPartOrgans(target).Values.ToArray())
                {
                    if (!entityManager.TryGetComponent<InternalChildOrganComponent>(organUid, out var organ))
                        continue;

                    foreach (var modifier in organ.IntegrityModifiers.ToArray())
                    {
                        if (healAmount <= 0)
                            return;

                        if (modifier.Value <= healAmount)
                        {
                            healAmount -= modifier.Value;
                            trauma.TryRemoveOrganDamageModifier((organUid, organ), modifier.Key.Item2, modifier.Key.Item1);
                        }
                        else
                        {
                            trauma.TryChangeOrganDamageModifier((organUid, organ), -healAmount, modifier.Key.Item2, modifier.Key.Item1);
                            return;
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
    }
}
