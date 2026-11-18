using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class TendWoundsEffect : SurgeryStepEffect
{
    [DataField]
    public ProtoId<DamageGroupPrototype> MainGroup = "Brute";

    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public float HealMultiplier = 0.07f;

    [DataField]
    public float DeadModifier = 0.2f;

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        var wounds = entityManager.System<WoundSystem>();
        var severity = wounds.GetWoundableSeverityPoint(target, MainGroup, healable: true, ignoreBlockers: true);
        if (severity <= 0)
            return true;

        var bonus = HealMultiplier * severity;
        if (entityManager.System<MobStateSystem>().IsDead(GetPatient(target, entityManager)))
            bonus *= DeadModifier;

        var adjusted = new DamageSpecifier();
        foreach (var (type, amount) in Damage.DamageDict)
        {
            adjusted.DamageDict[type] = amount - bonus;
        }

        entityManager.System<DamageableSystem>().ChangeDamage(target, adjusted, true, origin: surgeon, ignoreBlockers: true);
        return true;
    }
}
