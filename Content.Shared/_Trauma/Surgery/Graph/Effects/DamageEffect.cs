using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable, NetSerializable]
public sealed partial class DamageEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public bool IgnoreResistances = true;

    [DataField]
    public float SleepModifier = 0.5f;

    public override void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var damage = new DamageSpecifier(Damage);
        var body = entityManager.System<BodySystem>().GetBody(target) ?? target;
        if (entityManager.System<StatusEffectsSystem>().HasEffectComp<ForcedSleepingStatusEffectComponent>(body))
            damage *= SleepModifier;

        entityManager.System<DamageableSystem>()
            .ChangeDamage(target, damage, IgnoreResistances, origin: surgeon, ignoreBlockers: true);
    }
}
