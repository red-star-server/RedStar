using Content.Shared._RedStar.Genetics;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Components.Container;

[RegisterComponent]
public sealed partial class SampleContainerComponent : Component
{
    [ViewVariables]
    public bool Empty => Samples.Count == 0;

    [DataField, ViewVariables]
    public List<BiologicalSample> Samples = [];

    [DataField]
    public List<SampleGenerationEntry> InitialSamples = [];

    [DataField]
    public bool InitialSamplesInitialized;

    [DataField]
    public int MaxSamples = int.MaxValue;

    [DataField]
    public bool AllowTransfer = true;

    [DataField]
    public bool AllowCollection = true;

    [DataField]
    public EntityWhitelist? ToolsTransferWhitelist;
}

[DataDefinition]
public sealed partial class SampleGenerationEntry
{
    [DataField(required: true)]
    public ProtoId<GenomePrototype> Genome;

    [DataField]
    public float Stability = 1f;
}
