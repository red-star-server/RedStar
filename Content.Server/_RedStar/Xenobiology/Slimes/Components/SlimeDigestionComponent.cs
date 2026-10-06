using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RedStar.Xenobiology.Slimes.Components;

/// <summary>
/// Holds one incapacitated victim and gradually converts damage into nutrition.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SlimeDigestionComponent : Component
{
    public const string ContainerId = "slime-stomach";

    public ContainerSlot Stomach = default!;
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
    public TimeSpan DigestInterval = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan ConsumeDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public float ConsumeRange = 1.5f;

    [DataField]
    public EntityWhitelist? PreyWhitelist;

    [DataField]
    public FixedPoint2 FullyDigestedCellularDamage = 200;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDigestTime;
}
