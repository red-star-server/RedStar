using Content.Shared._Trauma.Traumas;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.EntityEffects;

/// <summary>
/// Used in yaml to set how much bone damage a reagent or chemical heals.
/// </summary>
public sealed partial class HealBoneDamage : EntityEffectBase<HealBoneDamage>
{
    /// <summary>
    /// Bone integrity restored when this effect is applied.
    /// </summary>
    [DataField]
    public FixedPoint2 Amount = 1;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-heal-bone-damage", ("chance", Probability), ("amount", Amount.Float()));
}

/// <summary>
/// Heals bone damage on every body part with a bone.
/// </summary>
public sealed partial class HealBoneDamageEffectSystem : EntityEffectSystem<BodyComponent, HealBoneDamage>
{
    [Dependency] private TraumaSystem _trauma = default!;
    [Dependency] private BodySystem _body = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<HealBoneDamage> args)
    {
        var amount = args.Effect.Amount * args.Scale;

        foreach (var bone in _body.EnumerateOrgans<BoneComponent>(ent))
        {
            _trauma.DamageBone((bone.Owner, bone.Comp2), -amount);
        }
    }
}
