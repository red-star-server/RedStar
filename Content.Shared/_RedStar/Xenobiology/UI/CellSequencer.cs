using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum CellSequencerUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiState(
    CellView[] insideCell,
    CellEntryView[] remoteCells,
    bool hasContainer,
    bool hasSpace,
    int dishRevision,
    NetEntity? dish)
    : BoundUserInterfaceState
{
    public readonly CellView[] InsideCells = insideCell;
    public readonly CellEntryView[] RemoteCells = remoteCells;
    public bool Connected;
    public readonly bool HasContainer = hasContainer;
    public readonly bool HasSpace = hasSpace;
    public readonly int DishRevision = dishRevision;
    public readonly NetEntity? Dish = dish;
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiAddMessage(int? cellIndex, int dishRevision, NetEntity? dish) : BoundUserInterfaceMessage
{
    public readonly int? CellIndex = cellIndex;
    public readonly int DishRevision = dishRevision;
    public readonly NetEntity? Dish = dish;
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiRemoveMessage(int? id, bool remote, int dishRevision, NetEntity? dish) : BoundUserInterfaceMessage
{
    public readonly int? Id = id;
    public readonly bool Remote = remote;
    public readonly int DishRevision = dishRevision;
    public readonly NetEntity? Dish = dish;
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiWithdrawMessage(int? id, int dishRevision, NetEntity? dish) : BoundUserInterfaceMessage
{
    public readonly int? Id = id;
    public readonly int DishRevision = dishRevision;
    public readonly NetEntity? Dish = dish;
}
