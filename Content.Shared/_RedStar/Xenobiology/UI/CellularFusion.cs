using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum CellularFusionUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CellularFusionUiSpliceMessage(int sampleAIndex, int sampleBIndex, int dishRevision, NetEntity? dish)
    : BoundUserInterfaceMessage
{
    public readonly int SampleAIndex = sampleAIndex;
    public readonly int SampleBIndex = sampleBIndex;
    public readonly int DishRevision = dishRevision;
    public readonly NetEntity? Dish = dish;
}

[Serializable, NetSerializable]
public sealed class CellularFusionUiState(
    SampleView[] samples,
    int dishRevision,
    NetEntity? dish,
    bool spliceInProgress,
    double spliceDuration,
    double spliceRemaining) : BoundUserInterfaceState
{
    public readonly SampleView[] Samples = samples;
    public readonly int DishRevision = dishRevision;
    public readonly NetEntity? Dish = dish;
    public readonly bool SpliceInProgress = spliceInProgress;
    public readonly double SpliceDuration = spliceDuration;
    public readonly double SpliceRemaining = spliceRemaining;
}
