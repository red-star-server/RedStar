using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Components.Container;

/// <summary>
/// Generates configured samples with individual stability when a container is created.
/// </summary>
[RegisterComponent]
public sealed partial class SampleGenerationComponent : Component
{
    [DataField]
    public List<SampleGenerationEntry> Samples = [];
}

[DataDefinition]
public sealed partial class SampleGenerationEntry
{
    [DataField(required: true)]
    public ProtoId<GenomePrototype> Genome;

    [DataField]
    public float Stability = 1f;
}
