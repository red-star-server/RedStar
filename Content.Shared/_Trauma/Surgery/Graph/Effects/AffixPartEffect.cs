using Content.Shared._Trauma.Surgery.Steps.Parts;
using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class AffixPartEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;

    [DataField]
    public float HealAmount = 12f;

    private static readonly ProtoId<DamageGroupPrototype> Brute = "Brute";

    public override void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>();
        var patient = body.GetBody(target) ?? target;
        if (body.GetOrgan(patient, Category) is not { } part)
            return;

        entityManager.System<WoundSystem>().TryHealWounds(part, HealAmount, out _, damageGroup: Brute);
        entityManager.RemoveComponent<OrganReattachedComponent>(part);
    }
}
