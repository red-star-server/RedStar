using System.Collections.Immutable;
using System.Linq;
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
/// Cells are immutable values: equality compares their fields, while database identity
/// is assigned separately by <see cref="CellEntry.Id"/>. Equal generated samples may
/// therefore still be separate database entries.
/// </remarks>
/// <seealso cref="CellPrototype"/>
/// <seealso cref="CellContainerComponent"/>
/// <seealso cref="SharedCellSystem"/>
[Serializable, NetSerializable]
public sealed class Cell(
    ProtoId<CellPrototype>? prototypeId,
    Color color,
    string name,
    float stability,
    int cost,
    IEnumerable<ProtoId<CellTraitPrototype>> traits)
    : IEquatable<Cell>
{
    /// <summary>
    /// Reflects the prototype on which the cell is based,
    /// if it is generated, this value will be null.
    /// </summary>
    [ViewVariables]
    public readonly ProtoId<CellPrototype>? PrototypeId = prototypeId;

    /// <summary>
    /// The color of a cell
    /// affecting only its display in the world or consoles.
    /// </summary>
    /// <seealso cref="SharedCellSystem.GetMergedColor"/>
    [ViewVariables]
    public readonly Color Color = color;

    /// <summary>
    /// Cell name, this can be changed by the player,
    /// don't use this to index cells.
    /// </summary>
    /// <seealso cref="SharedCellSystem.GetMergedName"/>
    [ViewVariables]
    public readonly string Name = name;

    /// <summary>
    /// The current stability of the cell,
    /// this affects the chance of successful splice and many other factors.
    /// </summary>
    ///
    /// <seealso cref="SharedCellSystem.GetMergedStability"/>
    [ViewVariables]
    public readonly float Stability = stability;

    /// <summary>
    /// Cost in materials of the device for printing as well as splicing of the cell.
    /// </summary>
    /// <seealso cref="SharedCellSystem.GetMergedCost"/>
    [ViewVariables]
    public readonly int Cost = cost;

    /// <summary>
    /// Trait prototypes carried by this cell.
    /// They affect an organism when injected and are tracked by <see cref="CellTraitHostComponent"/>.
    /// </summary>
    [ViewVariables]
    public readonly ImmutableArray<ProtoId<CellTraitPrototype>> Traits = ImmutableArray.CreateRange(traits);

    public Cell(Cell cell) : this(cell.PrototypeId, cell.Color, cell.Name, cell.Stability, cell.Cost, cell.Traits) { }

    public Cell(CellPrototype cell) : this(cell.ID, cell.Color, Loc.GetString(cell.Name), cell.Stability, cell.Cost, cell.Traits) { }

    public override bool Equals(object? obj)
    {
        return obj is Cell other && Equals(other);
    }

    public bool Equals(Cell? other)
    {
        return other is not null &&
               other.PrototypeId == PrototypeId &&
               other.Color == Color &&
               other.Stability.Equals(Stability) &&
               other.Cost == Cost &&
               other.Traits.SequenceEqual(Traits) &&
               other.Name == Name;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(PrototypeId);
        hash.Add(Color);
        hash.Add(Stability);
        hash.Add(Cost);
        hash.Add(Name);
        foreach (var trait in Traits)
        {
            hash.Add(trait);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(Cell? cellA, Cell? cellB)
    {
        return ReferenceEquals(cellA, cellB) || cellA is not null && cellA.Equals(cellB);
    }

    public static bool operator !=(Cell? cellA, Cell? cellB)
    {
        return !(cellA == cellB);
    }
}
