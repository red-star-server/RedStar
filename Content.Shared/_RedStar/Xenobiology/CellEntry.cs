using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology;

[Serializable, NetSerializable]
public readonly struct CellEntry(int id, Cell cell)
{
    public readonly int Id = id;
    public readonly Cell Cell = cell;
}
