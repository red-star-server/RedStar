namespace Content.Shared._RedStar.Xenobiology.Systems;

public abstract partial class SharedCellSystem
{
    public const int StabilizationCost = 5;

    public static float GetStabilizedStability(Cell cellA, Cell cellB)
    {
        var max = Math.Clamp(Math.Max(cellA.Stability, cellB.Stability), 0f, 1f);
        return Math.Min(1f, Math.Max(MathF.BitIncrement(max), max + (1f - max) * 0.04f));
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
