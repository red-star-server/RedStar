using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Xenobiology.Slimes.Components;

/// <summary>
/// Slime-specific mutation chance and environmental mutation routes.
/// </summary>
[RegisterComponent]
public sealed partial class SlimeMutationComponent : Component
{
    [DataField]
    public FixedPoint2 MutationChance = 0.25;

    [DataField]
    public List<SlimeMutationEntry> Mutations = [];

    public float[] MutationProgress = [];

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
