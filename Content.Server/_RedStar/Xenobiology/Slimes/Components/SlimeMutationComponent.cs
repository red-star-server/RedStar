using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Xenobiology.Slimes.Components;

/// <summary>
/// Environmental mutation routes and their adult-specific progress.
/// </summary>
[RegisterComponent]
public sealed partial class SlimeMutationComponent : Component
{
    [DataField]
    public List<SlimeMutationEntry> Mutations = [];

    public float[] MutationProgress = [];
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
