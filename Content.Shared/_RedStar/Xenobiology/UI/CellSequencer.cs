using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum CellSequencerUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiState(
    Cell[] insideCell,
    CellEntry[] remoteCells,
    int material,
    bool hasContainer,
    int dishRevision,
    NetEntity? dish)
    : BoundUserInterfaceState
{
    public readonly Cell[] InsideCells = insideCell;
    public readonly CellEntry[] RemoteCells = remoteCells;
    public bool Connected;
    public readonly int Material = material;
    public readonly bool HasContainer = hasContainer;
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
public sealed class CellSequencerUiReplaceMessage(int? id) : BoundUserInterfaceMessage
{
    public readonly int? Id = id;
}
