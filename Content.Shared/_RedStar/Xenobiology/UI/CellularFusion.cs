using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum CellularFusionUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CellularFusionUiSpliceMessage(int cellAId, int cellBId) : BoundUserInterfaceMessage
{
    public readonly int CellAId = cellAId;
    public readonly int CellBId = cellBId;
}

[Serializable, NetSerializable]
public sealed class CellularFusionUiState(
    CellEntryView[] remoteCells,
    int material,
    int spliceCost = 0,
    bool spliceInProgress = false,
    double spliceDuration = 0,
    double spliceRemaining = 0,
    CellView? lastResult = null)
    : BoundUserInterfaceState
{
    public readonly CellEntryView[] RemoteCells = remoteCells;
    public readonly int Material = material;
    public readonly int SpliceCost = spliceCost;
    public readonly bool SpliceInProgress = spliceInProgress;
    public readonly double SpliceDuration = spliceDuration;
    public readonly double SpliceRemaining = spliceRemaining;
    public readonly CellView? LastResult = lastResult;
}
