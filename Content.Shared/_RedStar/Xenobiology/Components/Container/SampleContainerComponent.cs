using Content.Shared.Whitelist;

namespace Content.Shared._RedStar.Xenobiology.Components.Container;

[RegisterComponent]
public sealed partial class SampleContainerComponent : Component
{
    [ViewVariables]
    public bool Empty => Samples.Count == 0;

    /// <summary>
    /// Current contents of the container.
    /// </summary>
    [DataField, ViewVariables]
    public List<BiologicalSample> Samples = [];

    [DataField]
    public int MaxSamples = int.MaxValue;

    /// <summary>
    /// Changes with sample contents or state to reject stale UI actions.
    /// </summary>
    [ViewVariables]
    public int Revision;

    /// <summary>
    /// Controls whether samples can be inserted into the container.
    /// </summary>
    [DataField]
    public bool AllowTransfer = true;

    /// <summary>
    /// Controls whether samples can be collected from the container.
    /// </summary>
    [DataField]
    public bool AllowCollection = true;

    [DataField]
    public EntityWhitelist? ToolsTransferWhitelist;
}
