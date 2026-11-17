using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class DamageEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public bool IgnoreResistances = true;

    [DataField]
    public float SleepModifier = 0.5f;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        var damage = new DamageSpecifier(Damage);
        if (IsAsleep(GetPatient(target, entityManager), entityManager))
            damage *= SleepModifier;

        entityManager.System<DamageableSystem>()
            .ChangeDamage(target, damage, IgnoreResistances, origin: surgeon, ignoreBlockers: true);
        return true;
    }
}
