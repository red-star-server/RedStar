using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology;

[Serializable, NetSerializable]
public enum SlimeStage : byte
{
    Baby,
    Adult
}

/// <summary>
/// Per-slime growth state and lineage configuration. Growth accumulates over time while fed.
/// </summary>
[RegisterComponent]
public sealed partial class SlimeLifecycleComponent : Component
{
    [DataField]
    public SlimeStage Stage = SlimeStage.Adult;

    [DataField(required: true)]
    public EntProtoId AdultPrototype;

    [DataField(required: true)]
    public EntProtoId BabyPrototype;

    [DataField]
    public float Growth;

    [DataField]
    public FixedPoint2 MutationChance = 0.25;

    [DataField]
    public List<SlimeMutationEntry> Mutations = new();

    public float[] MutationProgress = [];

    [DataField]
    public float GrowthRate = 1f;

    [DataField]
    public float GrowthThreshold = 100f;

    [DataField]
    public SatiationValue RequiredSatiation = "Peckish";

    [DataField]
    public int OffspringCount = 2;

    [DataField]
    public float MutationVariance = 0.05f;

    [DataField]
    public float MinimumMutationChance;

    [DataField]
    public float MaximumMutationChance = 1f;
}

[DataDefinition]
public sealed partial class SlimeMutationEntry
{
    [DataField(required: true)]
    public EntProtoId Target;

    [DataField]
    public float Weight = 1f;

    [DataField]
    public float ProgressRate = 1f;

    [DataField]
    public float RequiredProgress = 30f;

    [DataField]
    public EntityCondition[] Conditions = [];
}
