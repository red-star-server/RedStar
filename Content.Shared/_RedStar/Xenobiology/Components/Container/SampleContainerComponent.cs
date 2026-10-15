using Content.Shared.Whitelist;

namespace Content.Shared._RedStar.Xenobiology.Components.Container;

[RegisterComponent]
public sealed partial class SampleContainerComponent : Component
{
    [ViewVariables]
    public bool Empty => Samples.Count == 0;

    [DataField, ViewVariables]
    public List<BiologicalSample> Samples = [];

    [DataField]
    public int MaxSamples = int.MaxValue;

    /// <summary>
    /// Changes with sample contents or state to reject stale UI actions.
    /// </summary>
    [ViewVariables]
    public int Revision;

    [DataField]
    public bool AllowTransfer = true;

    [DataField]
    public bool AllowCollection = true;

    [DataField]
    public EntityWhitelist? ToolsTransferWhitelist;
}
