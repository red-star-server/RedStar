namespace Content.Shared._RedStar.Xenobiology.Systems;

public abstract partial class SharedCellSystem
{
    public static float GetStabilizedStability(Cell cellA, Cell cellB)
    {
        var max = Math.Max(cellA.Stability, cellB.Stability);
        var min = Math.Min(cellA.Stability, cellB.Stability);
        var gain = (1f - max) * (0.08f + 0.04f * min);
        return Math.Min(1f, max + gain);
    }

    public static string GetStabilizedName(Cell cellA, Cell cellB)
    {
        if (cellA.Name == cellB.Name)
            return cellA.Name;

        var nameA = cellA.Name[..(cellA.Name.Length / 2)];
        var nameB = cellB.Name[(cellB.Name.Length / 2)..];
        return $"{nameA}{nameB}";
    }

    public static Color GetStabilizedColor(Cell cellA, Cell cellB)
    {
        return Color.InterpolateBetween(cellA.Color, cellB.Color, 0.5f);
    }
}
