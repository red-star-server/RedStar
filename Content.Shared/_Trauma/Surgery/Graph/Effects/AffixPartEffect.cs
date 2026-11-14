using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class AffixPartEffect : SurgeryStepEffect
{
    /// <summary>
    /// Fixed category; when unset, the procedure instance's organ is used.
    /// </summary>
    [DataField]
    public ProtoId<OrganCategoryPrototype>? Category;

    [DataField]
    public float HealAmount = 12f;

    private static readonly ProtoId<DamageGroupPrototype> Brute = "Brute";

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        if ((Category ?? organ) is not { } category)
            return false;

        var body = entityManager.System<BodySystem>();
        var patient = body.GetBody(target) ?? target;
        if (body.GetOrgan(patient, category) is not { } part)
            return true;

        entityManager.System<WoundSystem>().TryHealWounds(part, HealAmount, out _, damageGroup: Brute);
        entityManager.RemoveComponent<OrganReattachedComponent>(part);
        return true;
    }
}