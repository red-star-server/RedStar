using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.EntityConditions;

/// <summary>
/// Checks the total positive damage of explicitly selected types, without counting unrelated injuries.
/// </summary>
public sealed partial class DamageTypesConditionSystem : EntityConditionSystem<DamageableComponent, DamageTypesCondition>
{
    [Dependency] private DamageableSystem _damage = default!;

    protected override void Condition(Entity<DamageableComponent> entity, ref EntityConditionEvent<DamageTypesCondition> args)
    {
        var damage = _damage.GetPositiveDamage(entity);
        var total = FixedPoint2.Zero;
        foreach (var type in args.Condition.Types)
        {
            if (damage.DamageDict.TryGetValue(type, out var amount) && amount > 0)
                total += amount;
        }
        args.Result = total >= args.Condition.Minimum;
    }
}

/// <inheritdoc cref="DamageTypesConditionSystem"/>
public sealed partial class DamageTypesCondition : EntityConditionBase<DamageTypesCondition>
{
    /// <summary>
    /// Damage types included in the total. Other injuries do not satisfy this condition.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<DamageTypePrototype>> Types = [];

    /// <summary>
    /// Minimum combined positive damage required across the selected types.
    /// </summary>
    [DataField]
    public FixedPoint2 Minimum = FixedPoint2.New(0.01);

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-damage-types", ("amount", Minimum));
}
