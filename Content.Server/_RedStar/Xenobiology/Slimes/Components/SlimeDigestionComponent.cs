using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Xenobiology.Slimes.Components;

/// <summary>
/// Controls short-range slime feeding through a timed bite.
/// </summary>
[RegisterComponent]
public sealed partial class SlimeDigestionComponent : Component
{
    public DoAfterId? ConsumeDoAfter;

    [DataField]
    public EntProtoId ConsumeAction = "ActionSlimeConsume";

    [DataField]
    public EntityUid? ConsumeActionEntity;

    [DataField]
    public DamageSpecifier DigestDamage = new() { DamageDict = new() { ["Cellular"] = 2 } };

    [DataField]
    public FixedPoint2 NutritionPerTick = 10;

    [DataField]
    public TimeSpan ConsumeDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public float ConsumeRange = 1.5f;

    [DataField]
    public EntityWhitelist? PreyWhitelist;

    [DataField]
    public FixedPoint2 FullyDigestedCellularDamage = 200;
}
