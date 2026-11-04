using Content.Shared.Damage.Components;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.EntityConditions;

public sealed partial class StaminaDamageCondition : EntityConditionBase<StaminaDamageCondition>
{
    /// <summary>
    /// Exclusive lower bound on stamina damage.
    /// </summary>
    [DataField]
    public float Min;

    /// <summary>
    /// Exclusive upper bound on stamina damage.
    /// </summary>
    [DataField]
    public float Max = float.PositiveInfinity;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString("reagent-effect-condition-guidebook-stamina-damage-threshold",
            ("max", float.IsPositiveInfinity(Max) ? (float) int.MaxValue : Max),
            ("min", Min));
}

public sealed partial class StaminaDamageConditionSystem : EntityConditionSystem<StaminaComponent, StaminaDamageCondition>
{
    protected override void Condition(Entity<StaminaComponent> ent, ref EntityConditionEvent<StaminaDamageCondition> args)
    {
        var total = ent.Comp.StaminaDamage;
        var min = args.Condition.Min;
        var max = args.Condition.Max;
        args.Result = total > min && total < max;
    }
}
