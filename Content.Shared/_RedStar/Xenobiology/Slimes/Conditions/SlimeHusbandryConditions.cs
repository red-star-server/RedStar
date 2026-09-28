using Content.Shared._RedStar.Xenobiology.Slimes;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Slimes.Conditions;

public sealed partial class SlimeTemperamentCondition : EntityConditionBase<SlimeTemperamentCondition>
{
    [DataField]
    public SlimeTemperament Temperament = SlimeTemperament.Calm;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => string.Empty;
}

public sealed partial class SlimeOvercrowdingCondition : EntityConditionBase<SlimeOvercrowdingCondition>
{
    [DataField]
    public int MinNearby = 1;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => string.Empty;
}
