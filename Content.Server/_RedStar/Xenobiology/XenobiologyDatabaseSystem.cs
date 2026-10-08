using System.Diagnostics.CodeAnalysis;
using Content.Server._RedStar.Xenobiology.Components;
using Content.Server._RedStar.Xenobiology.Events;
using Content.Server.Power.EntitySystems;
using Content.Server.Research.Systems;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared.Power;
using Content.Shared.Research.Components;

namespace Content.Server._RedStar.Xenobiology;

/// <summary>
/// Uses existing research connections to access an independent genome library.
/// </summary>
public sealed partial class XenobiologyDatabaseSystem : EntitySystem
{
    [Dependency] private ResearchSystem _research = default!;

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

    public bool AddCell(EntityUid machine, Cell cell)
    {
        if (!TryGetDatabase(machine, out var database))
        {
            return false;
        }

        var nextId = database.Value.Comp.NextCellId++;
        database.Value.Comp.Cells.Add(new CellEntry(nextId, cell));
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
