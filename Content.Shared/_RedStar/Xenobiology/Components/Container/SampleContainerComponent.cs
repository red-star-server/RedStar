using Content.Shared._RedStar.Genetics;
using Robust.Shared.GameStates;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Components.Container;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SampleContainerComponent : Component
{
    [ViewVariables]
    public bool Empty => SampleCount == 0;

    /// <summary>
    /// Number of stored samples, replicated without their genetic contents.
    /// Updated by SampleSystem when the collection changes.
    /// </summary>
    [AutoNetworkedField]
    public int SampleCount;

    /// <summary>
    /// Server-owned sample collection. Use SampleSystem to modify it and notify observers.
    /// </summary>
    [DataField, ViewVariables]
    public List<BiologicalSample> Samples = [];

    /// <summary>
    /// Samples generated once at MapInit, in addition to any serialized samples.
    /// </summary>
    [DataField]
    public List<SampleGenerationEntry> InitialSamples = [];

    /// <summary>
    /// Prevents MapInit from regenerating samples in a saved or copied container.
    /// </summary>
    [DataField]
    public bool InitialSamplesInitialized;

    [DataField, AutoNetworkedField]
    public int MaxSamples = int.MaxValue;

    [DataField]
    public bool AllowTransfer = true;

    [DataField]
    public bool AllowCollection = true;

    /// <summary>
    /// Tools permitted to transfer samples into this container. Null forbids tool transfers.
    /// </summary>
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
