using System.Linq;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Systems;

/// <summary>
/// The system responsible for the operation, copy, translate, update and splicing of <see cref="Cell"/>
/// and the containers in which they are contained,
/// without visual effects, only direct interaction.
/// </summary>
/// <seealso cref="CellContainerComponent"/>
/// <seealso cref="CellGenerationComponent"/>
/// <seealso cref="SharedCellVisualsSystem"/>
[PublicAPI]
public abstract partial class SharedCellSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;

    [SubscribeLocalEvent]
    private void OnDishExamined(Entity<CellContainerVisualsComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !TryComp<CellContainerComponent>(ent, out var container))
            return;

        args.PushText(container.Empty
            ? Loc.GetString("cell-dish-examine-empty")
            : Loc.GetString("cell-dish-examine-contents",
                ("cells", string.Join(", ", container.Cells.Select(cell => cell.Name)))));
    }

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
        if (!Resolve(ent, ref ent.Comp))
            return false;

        ent.Comp.Cells.Add(cell);
        ent.Comp.Revision++;
        Dirty(ent);

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

        Dirty(ent);

        var ev = new CellContainerChangedEvent();
        RaiseLocalEvent(ent, ev);

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
    /// Copies cells from the source container to the destination container.
    /// </summary>
    /// <seealso cref="AddCell(Entity{CellContainerComponent?}, Cell)"/>
    public void CopyCells(Entity<CellContainerComponent?> destination, Entity<CellContainerComponent?> source)
    {
        if (destination.Owner == source.Owner ||
            !Resolve(destination, ref destination.Comp) || !Resolve(source, ref source.Comp))
            return;

        foreach (var cell in source.Comp.Cells)
        {
            AddCell(destination, cell);
        }
    }

    /// <summary>
    /// Moves all cells from the source container to the destination container.
    /// </summary>
    public void MoveCells(Entity<CellContainerComponent?> source, Entity<CellContainerComponent?> destination)
    {
        if (source.Owner == destination.Owner ||
            !Resolve(source, ref source.Comp) || !Resolve(destination, ref destination.Comp))
            return;

        CopyCells(destination, source);
        ClearCells(source);
    }
}
