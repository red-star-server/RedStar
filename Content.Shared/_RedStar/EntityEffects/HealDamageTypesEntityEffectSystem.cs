using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.EntityEffects;

/// <summary>
/// Distributes a single healing budget across the specified injured damage types.
/// Types outside the list never receive healing, even when they belong to the same damage group.
/// </summary>
public sealed partial class HealDamageTypesEntityEffectSystem : EntityEffectSystem<DamageableComponent, HealDamageTypes>
{
    [Dependency] private DamageableSystem _damage = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<HealDamageTypes> args)
    {
        var damage = _damage.GetPositiveDamage(entity);
        var total = FixedPoint2.Zero;
        foreach (var type in args.Effect.Types)
        {
            if (damage.DamageDict.TryGetValue(type, out var amount) && amount > 0)
                total += amount;
        }

        var budget = FixedPoint2.Min(total, args.Effect.Amount * args.Scale);
        if (total <= 0 || budget <= 0)
            return;

        var change = new DamageSpecifier();
        var remaining = budget;
        var remainingDamage = total;
        foreach (var type in args.Effect.Types)
        {
            if (!damage.DamageDict.TryGetValue(type, out var amount) || amount <= 0)
                continue;

            var healing = FixedPoint2.Min(amount, remaining * (amount.Float() / remainingDamage.Float()));
            change.DamageDict[type] = -healing;
            remaining -= healing;
            remainingDamage -= amount;
        }
        _damage.TryChangeDamage(entity.AsNullable(), change, ignoreResistances: true, interruptsDoAfters: false);
    }
}

/// <inheritdoc cref="HealDamageTypesEntityEffectSystem"/>
public sealed partial class HealDamageTypes : EntityEffectBase<HealDamageTypes>
{
    /// <summary>
    /// Injuries eligible for the shared healing budget.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<DamageTypePrototype>> Types = [];

    /// <summary>
    /// Total positive amount to heal per application, before the caller's scale is applied.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Amount;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-heal-damage-types", ("chance", Probability), ("amount", Amount),
            ("types", ContentLocalizationManager.FormatList(Types.Select(type => prototype.Index(type).LocalizedName).ToList())));
}
