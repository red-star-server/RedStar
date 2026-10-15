using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum CellularFusionUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CellularFusionUiSpliceMessage(int sampleAId, int sampleBId)
    : BoundUserInterfaceMessage
{
    public readonly int SampleAId = sampleAId;
    public readonly int SampleBId = sampleBId;
}

[Serializable, NetSerializable]
public sealed class CellularFusionUiState(
    SampleView[] samples,
    NetEntity? dish,
    bool spliceInProgress,
    double spliceDuration,
    double spliceRemaining) : BoundUserInterfaceState
{
    public readonly SampleView[] Samples = samples;
    public readonly NetEntity? Dish = dish;
    public readonly bool SpliceInProgress = spliceInProgress;
    public readonly double SpliceDuration = spliceDuration;
    public readonly double SpliceRemaining = spliceRemaining;
}
