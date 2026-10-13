using System.Diagnostics.CodeAnalysis;
using Content.Server._RedStar.Xenobiology.Components;
using Content.Server._RedStar.Xenobiology.Events;
using Content.Server._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Systems;
using Content.Server.Power.EntitySystems;
using Content.Server.Research.Systems;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared.Power;
using Content.Shared.Research.Components;

namespace Content.Server._RedStar.Xenobiology;

/// <summary>
/// Uses existing research connections to store physical samples.
/// </summary>
public sealed partial class XenobiologyDatabaseSystem : EntitySystem
{
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private SharedCellSystem _cells = default!;

    public bool TryGetDatabase(EntityUid machine,
        [NotNullWhen(true)] out Entity<XenobiologyDatabaseComponent>? database)
    {
        database = null;
        if (!_research.TryGetClientServer(machine, out var server, out _) ||
            TerminatingOrDeleted(server.Value) ||
            !this.IsPowered(server.Value, EntityManager) ||
            !TryComp<XenobiologyDatabaseComponent>(server.Value, out var component))
        {
            return false;
        }

        database = (server.Value, component);
        return true;
    }

    public bool TryGetCell(EntityUid machine, int id, [NotNullWhen(true)] out Cell? cell)
    {
        cell = null;
        if (!TryGetDatabase(machine, out var database))
        {
            return false;
        }

        foreach (var entry in database.Value.Comp.Cells)
        {
            if (entry.Id != id)
                continue;

            cell = entry.Cell;
            return true;
        }

        return false;
    }

    public bool StoreCell(EntityUid machine, Entity<CellContainerComponent> dish, int index)
    {
        if (!TryGetDatabase(machine, out var database) || index < 0 || index >= dish.Comp.Cells.Count)
            return false;

        var cell = dish.Comp.Cells[index];
        var genome = cell.GetGenome();
        if (cell.HasGenome && (genome is null ||
            _genomes.ValidateSnapshot(genome) != GenomeOperationResult.Success))
            return false;

        if (!_cells.RemoveCell((dish.Owner, dish.Comp), cell))
            return false;
        var nextId = database.Value.Comp.NextCellId++;
        database.Value.Comp.Cells.Add(new CellEntry(nextId, cell));
        NotifyClients(database.Value.Owner);
        return true;
    }

    public bool WithdrawCell(EntityUid machine, int id, Entity<CellContainerComponent> dish)
    {
        if (!TryGetDatabase(machine, out var database))
            return false;

        var index = database.Value.Comp.Cells.FindIndex(entry => entry.Id == id);
        if (index < 0 || dish.Comp.Cells.Count >= dish.Comp.MaxCells ||
            !_cells.AddCell((dish.Owner, dish.Comp), database.Value.Comp.Cells[index].Cell))
            return false;

        database.Value.Comp.Cells.RemoveAt(index);
        NotifyClients(database.Value.Owner);
        return true;
    }

    public bool CompletePair(EntityUid machine, int firstId, int secondId,
        Entity<CellContainerComponent> dish, Cell? result)
    {
        if (firstId == secondId || !TryGetDatabase(machine, out var database))
            return false;

        var entries = database.Value.Comp.Cells;
        var firstIndex = entries.FindIndex(entry => entry.Id == firstId);
        var secondIndex = entries.FindIndex(entry => entry.Id == secondId);
        if (firstIndex < 0 || secondIndex < 0 ||
            result is not null && dish.Comp.Cells.Count >= dish.Comp.MaxCells)
            return false;

        var high = Math.Max(firstIndex, secondIndex);
        var low = Math.Min(firstIndex, secondIndex);
        var highEntry = entries[high];
        var lowEntry = entries[low];
        entries.RemoveAt(high);
        entries.RemoveAt(low);
        if (result is not null && !_cells.AddCell((dish.Owner, dish.Comp), result))
        {
            entries.Insert(low, lowEntry);
            entries.Insert(high, highEntry);
            return false;
        }

        NotifyClients(database.Value.Owner);
        return true;
    }

    public bool RemoveCell(EntityUid machine, int id)
    {
        if (!TryGetDatabase(machine, out var database))
        {
            return false;
        }

        var index = database.Value.Comp.Cells.FindIndex(entry => entry.Id == id);
        if (index < 0)
            return false;

        database.Value.Comp.Cells.RemoveAt(index);
        NotifyClients(database.Value.Owner);
        return true;
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<XenobiologyDatabaseComponent> ent, ref PowerChangedEvent args)
    {
        NotifyClients(ent.Owner);
    }

    private void NotifyClients(EntityUid server)
    {
        if (!TryComp<ResearchServerComponent>(server, out var component))
        {
            return;
        }

        foreach (var client in component.Clients.ToArray())
        {
            if (!TerminatingOrDeleted(client))
            {
                RaiseLocalEvent(client, new XenobiologyDatabaseChangedEvent());
            }
        }
    }
}
