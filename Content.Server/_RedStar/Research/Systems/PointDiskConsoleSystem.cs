using Content.Server._RedStar.Research.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Research.Disk;
using Content.Server.Research.Systems;
using Content.Shared._RedStar.Research;
using Content.Shared.Power;
using Content.Shared.Research.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Research.Systems;

/// <summary>
/// Withdraws research points as disks using the existing research client/server connection.
/// </summary>
public sealed partial class PointDiskConsoleSystem : EntitySystem
{
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PointDiskPrintingComponent, PointDiskConsoleComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var printing, out var console, out var metadata))
        {
            if (metadata.EntityPaused || printing.PowerLostAt != null || printing.FinishTime > _timing.CurTime)
                continue;

            var disk = printing.Disk;
            RemComp<PointDiskPrintingComponent>(uid);
            SpawnNextToOrDrop(disk, uid);
            UpdateInterface((uid, console));
        }
    }

    [SubscribeLocalEvent]
    private void OnPrintMessage(Entity<PointDiskConsoleComponent> ent, ref PointDiskConsolePrintMessage args)
    {
        TryPrintDisk(ent.AsNullable(), args.Disk);
    }

    /// <summary>
    /// Reserves points on the connected server and starts one print job.
    /// </summary>
    public bool TryPrintDisk(Entity<PointDiskConsoleComponent?> ent, EntProtoId disk)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (HasComp<PointDiskPrintingComponent>(ent) || !_power.IsPowered(ent))
            return false;

        if (!Transform(ent).Anchored || !ent.Comp.Disks.Contains(disk) || !TryGetDiskPoints(disk, out var cost))
            return false;

        if (!_research.TryGetClientServer(ent, out var server, out var research) || research.Points < cost)
            return false;

        // Reserve the job before changing points, which synchronously notifies research clients.
        var printing = AddComp<PointDiskPrintingComponent>(ent);
        printing.Disk = disk;
        printing.FinishTime = _timing.CurTime + ent.Comp.PrintDuration;
        _research.ModifyServerPoints(server.Value, -cost, research);
        _audio.PlayPvs(ent.Comp.PrintSound, ent);
        UpdateInterface((ent.Owner, ent.Comp));
        return true;
    }

    private bool TryGetDiskPoints(EntProtoId id, out int points)
    {
        points = 0;
        if (!ProtoMan.TryIndex(id, out var prototype) ||
            !prototype.TryComp<ResearchDiskComponent>(out var disk, EntityManager.ComponentFactory) ||
            disk.UnlockAllTech || disk.Points <= 0)
            return false;

        points = disk.Points;
        return true;
    }

    private void UpdateInterface(Entity<PointDiskConsoleComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, PointDiskConsoleUiKey.Key))
            return;

        var disks = new Dictionary<EntProtoId, int>();
        foreach (var id in ent.Comp.Disks)
        {
            if (TryGetDiskPoints(id, out var points))
                disks[id] = points;
        }

        var connected = _research.TryGetClientServer(ent, out _, out var server);
        _ui.SetUiState(ent.Owner, PointDiskConsoleUiKey.Key, new PointDiskConsoleState(
            disks,
            server?.Points ?? 0,
            connected,
            _power.IsPowered(ent),
            HasComp<PointDiskPrintingComponent>(ent)));
    }

    [SubscribeLocalEvent]
    private void OnBoundUIOpenedEvent(Entity<PointDiskConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey.Equals(PointDiskConsoleUiKey.Key))
            UpdateInterface(ent);
    }

    [SubscribeLocalEvent]
    private void OnPointsChangedEvent(
        Entity<PointDiskConsoleComponent> ent,
        ref ResearchServerPointsChangedEvent args)
    {
        UpdateInterface(ent);
    }

    [SubscribeLocalEvent]
    private void OnRegistrationChangedEvent(
        Entity<PointDiskConsoleComponent> ent,
        ref ResearchRegistrationChangedEvent args)
    {
        UpdateInterface(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChangedEvent(Entity<PointDiskConsoleComponent> ent, ref PowerChangedEvent args)
    {
        if (TryComp<PointDiskPrintingComponent>(ent, out var printing))
        {
            if (!args.Powered)
                printing.PowerLostAt ??= _timing.CurTime;
            else if (printing.PowerLostAt is { } lostAt)
            {
                printing.FinishTime += _timing.CurTime - lostAt;
                printing.PowerLostAt = null;
            }
        }

        UpdateInterface(ent);
    }
}
