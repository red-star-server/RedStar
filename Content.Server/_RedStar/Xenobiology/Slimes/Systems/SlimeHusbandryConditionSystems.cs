using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Shared._RedStar.Xenobiology.Slimes.Conditions;
using Content.Shared.EntityConditions;

namespace Content.Server._RedStar.Xenobiology.Slimes.Systems;

public sealed partial class SlimeTemperamentConditionSystem :
    EntityConditionSystem<SlimeHusbandryComponent, SlimeTemperamentCondition>
{
    protected override void Condition(Entity<SlimeHusbandryComponent> entity,
        ref EntityConditionEvent<SlimeTemperamentCondition> args)
    {
        args.Result = entity.Comp.Temperament == args.Condition.Temperament;
    }
}

public sealed partial class SlimeOvercrowdingConditionSystem :
    EntityConditionSystem<SlimeHusbandryComponent, SlimeOvercrowdingCondition>
{
    protected override void Condition(Entity<SlimeHusbandryComponent> entity,
        ref EntityConditionEvent<SlimeOvercrowdingCondition> args)
    {
        args.Result = entity.Comp.NearbySlimes >= args.Condition.MinNearby;
    }
}
