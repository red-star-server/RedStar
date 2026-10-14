using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology;

/// <summary>
/// Stores one laboratory sample and its complete genome.
/// The client receives only a separate CellView.
/// </summary>
/// <seealso cref="CellPrototype"/>
/// <seealso cref="CellContainerComponent"/>
/// <seealso cref="SharedCellSystem"/>
[DataDefinition]
public sealed partial class Cell
{
    /// <summary>
    /// Reflects the prototype on which the cell is based,
    /// if it is generated, this value will be null.
    /// </summary>
    [DataField, ViewVariables]
    public ProtoId<CellPrototype>? PrototypeId { get; private set; }

    /// <summary>
    /// The color of a cell
    /// affecting only its display in the world or consoles.
    /// </summary>
    /// <seealso cref="SharedCellSystem.GetStabilizedColor"/>
    [DataField, ViewVariables]
    public Color Color { get; private set; }

    /// <summary>
    /// Cell name, this can be changed by the player,
    /// don't use this to index cells.
    /// </summary>
    /// <seealso cref="SharedCellSystem.GetStabilizedName"/>
    [DataField, ViewVariables]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The current stability of the cell,
    /// this affects stabilization and genetic transfer.
    /// </summary>
    ///
    /// <seealso cref="SharedCellSystem.GetStabilizedStability"/>
    [DataField, ViewVariables]
    public float Stability { get; private set; }

    /// <summary>
    /// Genetic material is stored with the sample on the server.
    /// </summary>
    [DataField]
    private GenomeSnapshot? _genome;

    [ViewVariables]
    public int GeneCount => _genome?.GeneCount ?? 0;

    [ViewVariables]
    public bool HasGenome => _genome is not null;

    public Cell() { }

    public Cell(ProtoId<CellPrototype>? prototypeId, Color color, string name,
        float stability, GenomeSnapshot? genome)
    {
        PrototypeId = prototypeId;
        Color = color;
        Name = name;
        Stability = stability;
        _genome = genome?.Clone();
    }

    public GenomeSnapshot? GetGenome() => _genome?.Clone();

    public Cell(CellPrototype cell) : this(cell.ID, cell.Color, Loc.GetString(cell.Name), cell.Stability,
        new GenomeSnapshot(cell.GenomeId, cell.Genes)) { }

}
