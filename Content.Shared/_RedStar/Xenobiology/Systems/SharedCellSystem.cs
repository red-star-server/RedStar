using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Events;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Systems;

/// <summary>
/// Manages samples in cell containers.
/// Moves complete samples between containers.
/// Notifies other systems when contents change.
/// </summary>
/// <seealso cref="CellContainerComponent"/>
/// <seealso cref="CellGenerationComponent"/>
/// <seealso cref="SharedCellVisualsSystem"/>
[PublicAPI]
public abstract partial class SharedCellSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;

    /// <summary>
    /// Adds a new <see cref="Cell"/> instance created from the prototype to the container.
    /// </summary>
    /// <seealso cref="CellPrototype"/>
    public bool AddCell(Entity<CellContainerComponent?> ent, ProtoId<CellPrototype> cellId)
    {
        if (!Resolve(ent, ref ent.Comp) || !_prototype.TryIndex(cellId, out var cellPrototype))
            return false;

        return AddCell(ent, new Cell(cellPrototype));
    }

    /// <summary>
    /// Adds a new <see cref="Cell"/> instance to the container.
    /// </summary>
    public bool AddCell(Entity<CellContainerComponent?> ent, Cell cell)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Cells.Count >= ent.Comp.MaxCells)
            return false;

        ent.Comp.Cells.Add(cell);
        ent.Comp.Revision++;

        var ev = new CellContainerChangedEvent();
        RaiseLocalEvent(ent, ev);

        return true;
    }

    /// <summary>
    /// Remove a <see cref="Cell"/> instance from the container.
    /// </summary>
    public bool RemoveCell(Entity<CellContainerComponent?> ent, Cell cell)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.Cells.Remove(cell))
            return false;

        ent.Comp.Revision++;

        var ev = new CellContainerChangedEvent();
        RaiseLocalEvent(ent, ev);

        return true;
    }

    /// <summary>
    /// Replaces two distinct samples with one result and notifies listeners once.
    /// </summary>
    public bool ReplaceCells(Entity<CellContainerComponent?> ent, Cell first, Cell second, Cell result)
    {
        if (!Resolve(ent, ref ent.Comp) || result is null || ReferenceEquals(first, second) ||
            ReferenceEquals(result, first) || ReferenceEquals(result, second) ||
            !ent.Comp.Cells.Contains(first) || !ent.Comp.Cells.Contains(second) ||
            ent.Comp.Cells.Contains(result))
            return false;

        ent.Comp.Cells.Remove(first);
        ent.Comp.Cells.Remove(second);
        ent.Comp.Cells.Add(result);
        ent.Comp.Revision++;
        RaiseLocalEvent(ent, new CellContainerChangedEvent());
        return true;
    }

    /// <summary>
    /// Remove all <see cref="Cell"/> instances from the container.
    /// </summary>
    /// <seealso cref="RemoveCell"/>
    public void ClearCells(Entity<CellContainerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        while (ent.Comp.Cells.Count != 0)
        {
            RemoveCell(ent, ent.Comp.Cells[0]);
        }
    }

    /// <summary>
    /// Moves all cells only when the destination has room for every sample.
    /// </summary>
    public bool MoveCells(Entity<CellContainerComponent?> source, Entity<CellContainerComponent?> destination)
    {
        if (source.Owner == destination.Owner ||
            !Resolve(source, ref source.Comp) || !Resolve(destination, ref destination.Comp) ||
            source.Comp.Empty || destination.Comp.MaxCells - destination.Comp.Cells.Count < source.Comp.Cells.Count)
            return false;

        destination.Comp.Cells.AddRange(source.Comp.Cells);
        source.Comp.Cells.Clear();
        source.Comp.Revision++;
        destination.Comp.Revision++;
        RaiseLocalEvent(source, new CellContainerChangedEvent());
        RaiseLocalEvent(destination, new CellContainerChangedEvent());
        return true;
    }
}
