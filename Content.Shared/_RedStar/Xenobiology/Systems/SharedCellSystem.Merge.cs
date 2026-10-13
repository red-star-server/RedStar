namespace Content.Shared._RedStar.Xenobiology.Systems;

public abstract partial class SharedCellSystem
{
    public static float GetMergedStability(Cell cellA, Cell cellB)
    {
        var max = Math.Max(cellA.Stability, cellB.Stability);
        var delta = Math.Abs(cellA.Stability - cellB.Stability);
        var stability = max * (1 - delta) - max / 25;

        return Math.Clamp(stability, 0, 1);
    }

    public static string GetMergedName(Cell cellA, Cell cellB)
    {
        if (cellA.Name == cellB.Name)
            return cellA.Name;

        var nameA = cellA.Name[..(cellA.Name.Length / 2)];
        var nameB = cellB.Name[(cellB.Name.Length / 2)..];
        return $"{nameA}{nameB}";
    }

    public static Color GetMergedColor(Cell cellA, Cell cellB)
    {
        return Color.InterpolateBetween(cellA.Color, cellB.Color, 0.5f);
    }

    public static int GetMergedCost(Cell cellA, Cell cellB)
    {
        return GetCellCost(cellA.GeneCount + cellB.GeneCount);
    }

    public static int GetCellCost(int geneCount)
    {
        return 5 + geneCount * 3;
    }

}
