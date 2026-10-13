using System.Linq;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology;

/// <summary>
/// Displays the current information about the cell,
/// it may differ from the prototype due to changes and other mutations,
/// so to work with cells we allocate a separate class,
/// which is also passed between the client and the server.
/// </summary>
/// <remarks>
/// Compare sample contents with <see cref="SameContents"/> when needed.
/// Database identity is assigned separately by <see cref="CellEntry.Id"/>.
/// </remarks>
/// <seealso cref="CellPrototype"/>
/// <seealso cref="CellContainerComponent"/>
/// <seealso cref="SharedCellSystem"/>
[DataDefinition]
[Serializable, NetSerializable]
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
    /// <seealso cref="SharedCellSystem.GetMergedColor"/>
    [DataField, ViewVariables]
    public Color Color { get; private set; }

    /// <summary>
    /// Cell name, this can be changed by the player,
    /// don't use this to index cells.
    /// </summary>
    /// <seealso cref="SharedCellSystem.GetMergedName"/>
    [DataField, ViewVariables]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The current stability of the cell,
    /// this affects the chance of successful splice and many other factors.
    /// </summary>
    ///
    /// <seealso cref="SharedCellSystem.GetMergedStability"/>
    [DataField, ViewVariables]
    public float Stability { get; private set; }

    /// <summary>
    /// Cost in materials of the device for printing as well as splicing of the cell.
    /// </summary>
    /// <seealso cref="SharedCellSystem.GetMergedCost"/>
    [DataField, ViewVariables]
    public int Cost { get; private set; }

    /// <summary>
    /// Genetic material is kept on the server; networked laboratory views contain no gene identifiers.
    /// </summary>
    [DataField, NonSerialized]
    private GenomeSnapshot? _genome;

    [DataField, ViewVariables]
    public int GeneCount { get; private set; }

    [DataField, ViewVariables]
    public bool HasGenome { get; private set; }

    public Cell() { }

    public Cell(ProtoId<CellPrototype>? prototypeId, Color color, string name,
        float stability, int cost, GenomeSnapshot? genome)
    {
        PrototypeId = prototypeId;
        Color = color;
        Name = name;
        Stability = stability;
        Cost = cost;
        _genome = genome is null ? null : new GenomeSnapshot(genome.GenomeId, genome.GetGenes());
        GeneCount = genome?.GetGenes().Count ?? 0;
        HasGenome = genome is not null;
    }

    public GenomeSnapshot? GetGenome() => _genome is null ? null : new GenomeSnapshot(_genome.GenomeId, _genome.GetGenes());

    public Cell(Cell cell) : this(cell.PrototypeId, cell.Color, cell.Name, cell.Stability, cell.Cost, cell._genome) { }

    public Cell(CellPrototype cell) : this(cell.ID, cell.Color, Loc.GetString(cell.Name), cell.Stability, cell.Cost,
        new GenomeSnapshot(cell.GenomeId, cell.Genes)) { }

    public bool SameContents(Cell? other)
    {
        return other is not null &&
               other.PrototypeId == PrototypeId &&
               other.Color == Color &&
               other.Stability.Equals(Stability) &&
               other.Cost == Cost &&
               other.GeneCount == GeneCount &&
               other.HasGenome == HasGenome &&
               SameGenome(other._genome, _genome) &&
               other.Name == Name;
    }

    private static bool SameGenome(GenomeSnapshot? first, GenomeSnapshot? second)
    {
        if (first is null || second is null)
            return first is null && second is null;
        if (first.GenomeId != second.GenomeId)
            return false;
        var a = first.GetGenes();
        var b = second.GetGenes();
        return a.Count == b.Count && a.All(entry => b.TryGetValue(entry.Key, out var state) &&
            entry.Value.Allele == state.Allele && entry.Value.Number == state.Number);
    }
}
