using Content.Shared._RedStar.Genetics;
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
    /// Replaces two distinct samples with one result and notifies listeners once.
    /// </summary>
    public bool ReplaceCells(Entity<CellContainerComponent?> ent, Cell first, Cell second, Cell result)
    {
        if (!Resolve(ent, ref ent.Comp) || ReferenceEquals(first, second) ||
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
    /// Removes one gene from a contained sample and notifies observers once.
    /// </summary>
    public bool TryRemoveGene(Entity<CellContainerComponent?> ent, Cell cell, ProtoId<GenePrototype> id)
    {
        if (!Resolve(ent, ref ent.Comp) || !ent.Comp.Cells.Contains(cell) || !cell.TryRemoveGene(id))
            return false;

        ent.Comp.Revision++;
        RaiseLocalEvent(ent, new CellContainerChangedEvent());
        return true;
    }

    /// <summary>
    /// Remove all <see cref="Cell"/> instances from the container.
    /// </summary>
    public void ClearCells(Entity<CellContainerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Empty)
            return;

        ent.Comp.Cells.Clear();
        ent.Comp.Revision++;
        RaiseLocalEvent(ent, new CellContainerChangedEvent());
    }

    /// <summary>
    /// Moves the first sample from one container to another without an intermediate notification.
    /// </summary>
    public bool MoveFirstCell(Entity<CellContainerComponent?> source, Entity<CellContainerComponent?> destination)
    {
        if (source.Owner == destination.Owner ||
            !Resolve(source, ref source.Comp) || !Resolve(destination, ref destination.Comp) ||
            source.Comp.Empty || destination.Comp.Cells.Count >= destination.Comp.MaxCells)
            return false;

        var cell = source.Comp.Cells[0];
        source.Comp.Cells.RemoveAt(0);
        destination.Comp.Cells.Add(cell);
        source.Comp.Revision++;
        destination.Comp.Revision++;
        RaiseLocalEvent(source, new CellContainerChangedEvent());
        RaiseLocalEvent(destination, new CellContainerChangedEvent());
        return true;
    }
}
