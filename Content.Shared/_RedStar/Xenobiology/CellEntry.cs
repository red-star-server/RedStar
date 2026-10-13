namespace Content.Shared._RedStar.Xenobiology;

[DataDefinition]
public partial struct CellEntry
{
    [DataField]
    public int Id;

    [DataField]
    public Cell Cell;

    public CellEntry(int id, Cell cell)
    {
        Id = id;
        Cell = cell;
    }
}
