using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

/// <summary>
/// Laboratory information visible to clients. The genome stays in the server-side Cell.
/// </summary>
[Serializable, NetSerializable]
public sealed class CellView(Cell cell)
{
    public readonly string Name = cell.Name;
    public readonly Color Color = cell.Color;
    public readonly float Stability = cell.Stability;
    public readonly int Cost = cell.Cost;
    public readonly int GeneCount = cell.GeneCount;
    public readonly bool HasGenome = cell.HasGenome;

    public bool SameDisplay(CellView other) =>
        Name == other.Name && Color == other.Color && Stability.Equals(other.Stability) &&
        Cost == other.Cost && GeneCount == other.GeneCount && HasGenome == other.HasGenome;
}

[Serializable, NetSerializable]
public sealed class CellEntryView(int id, Cell cell)
{
    public readonly int Id = id;
    public readonly CellView Cell = new(cell);
}
