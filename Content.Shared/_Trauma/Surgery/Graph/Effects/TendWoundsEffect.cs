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

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        var wounds = entityManager.System<WoundSystem>();
        var severity = wounds.GetWoundableSeverityPoint(target, MainGroup, healable: true, ignoreBlockers: true);
        if (severity <= 0)
            return true;

        var bonus = HealMultiplier * severity;
        var body = entityManager.System<BodySystem>().GetBody(target) ?? target;
        if (entityManager.System<MobStateSystem>().IsDead(body))
            bonus *= 0.2f;

        var adjusted = new DamageSpecifier(Damage);
        var group = IoCManager.Resolve<IPrototypeManager>().Index(MainGroup);
        foreach (var type in group.DamageTypes)
        {
            adjusted.DamageDict[type] = adjusted.DamageDict.GetValueOrDefault(type) - bonus;
        }

        entityManager.System<DamageableSystem>().ChangeDamage(target, adjusted, true, origin: surgeon, ignoreBlockers: true);
        return true;
    }
}
