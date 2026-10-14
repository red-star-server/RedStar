using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum CellularFusionUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CellularFusionUiSpliceMessage(int cellAIndex, int cellBIndex, int dishRevision, NetEntity? dish)
    : BoundUserInterfaceMessage
{
    public readonly int CellAIndex = cellAIndex;
    public readonly int CellBIndex = cellBIndex;
    public readonly int DishRevision = dishRevision;
    public readonly NetEntity? Dish = dish;
}

[Serializable, NetSerializable]
public sealed class CellularFusionUiState(
    CellView[] cells,
    int dishRevision,
    NetEntity? dish,
    bool spliceInProgress,
    double spliceDuration,
    double spliceRemaining) : BoundUserInterfaceState
{
    public readonly CellView[] Cells = cells;
    public readonly int DishRevision = dishRevision;
    public readonly NetEntity? Dish = dish;
    public readonly bool SpliceInProgress = spliceInProgress;
    public readonly double SpliceDuration = spliceDuration;
    public readonly double SpliceRemaining = spliceRemaining;
}
