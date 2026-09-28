using System.Linq;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology;

public sealed partial class XenobiologyConsoleSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _links = default!;
    [Dependency] private XenobiologyCellScannerSystem _scanner = default!;
    [Dependency] private SlimeScanSystem _scan = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<XenobiologyCellScannerComponent> _scannerQuery;

    private static readonly ProtoId<SourcePortPrototype> ScannerPort = "XenobiologyConsole";
    private static readonly ProtoId<SinkPortPrototype> CellPort = "XenobiologyCellScanner";
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(0.75);
    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        // Do not enumerate consoles every frame. Closed consoles never perform spatial lookups.
        if (_timing.CurTime < _nextUpdate)
            return;
        _nextUpdate = _timing.CurTime + TimeSpan.FromSeconds(0.1);

        var query = EntityQueryEnumerator<XenobiologyConsoleComponent, ActiveUserInterfaceComponent>();
        while (query.MoveNext(out var uid, out var console, out _))
        {
            if (_timing.CurTime >= console.NextUpdate && _ui.IsUiOpen(uid, XenobiologyConsoleUiKey.Key))
                UpdateState(uid, console);
        }
    }

    [SubscribeLocalEvent]
    private void OnOpened(Entity<XenobiologyConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(XenobiologyConsoleUiKey.Key))
            return;

        var (uid, console) = ent;
        console.LastState = null;
        UpdateState(uid, console);
    }

    [SubscribeLocalEvent]
    private void OnClosed(Entity<XenobiologyConsoleComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(XenobiologyConsoleUiKey.Key))
            return;

        var console = ent.Comp;
        ClearSelection(console);
        console.LastState = null;
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(EntityUid uid, XenobiologyConsoleComponent console, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            ClearSelection(console);
        UpdateState(uid, console);
    }

    [SubscribeLocalEvent]
    private void OnNewLink(EntityUid uid, XenobiologyConsoleComponent console, NewLinkEvent args)
    {
        if (args.SourcePort == ScannerPort)
            console.NextUpdate = TimeSpan.Zero;
    }

    [SubscribeLocalEvent]
    private void OnDisconnected(EntityUid uid, XenobiologyConsoleComponent console, PortDisconnectedEvent args)
    {
        // DeviceLink raises this before removing its link. Refresh on the next update.
        if (args.Port == ScannerPort)
            console.NextUpdate = TimeSpan.Zero;
    }

    private bool CanControl(EntityUid uid, EntityUid actor)
    {
        return _ui.IsUiOpen(uid, XenobiologyConsoleUiKey.Key, actor) &&
               this.IsPowered(uid, EntityManager);
    }

    private HashSet<EntityUid> GetLinkedScanners(EntityUid console)
    {
        var result = _links.GetLinkedSinks(console, ScannerPort);
        result.RemoveWhere(uid => TerminatingOrDeleted(uid) ||
                                  !_scannerQuery.HasComp(uid) ||
                                  !HasComp<DeviceLinkSinkComponent>(uid) ||
                                  !_links.GetLinks(console, uid).Contains((ScannerPort, CellPort)));
        return result;
    }

    [SubscribeLocalEvent]
    private void OnSelectScanner(Entity<XenobiologyConsoleComponent> ent,
        ref XenobiologyConsoleSelectScannerMessage args)
    {
        var (uid, console) = ent;
        if (!args.UiKey.Equals(XenobiologyConsoleUiKey.Key) ||
            !CanControl(uid, args.Actor) ||
            !TryGetEntity(args.Scanner, out var scanner) ||
            !GetLinkedScanners(uid).Contains(scanner.Value))
            return;

        if (console.SelectedScanner != scanner)
        {
            console.SelectedScanner = scanner;
            console.SelectedSlime = null;
        }
        UpdateState(uid, console);
    }

    [SubscribeLocalEvent]
    private void OnSelectSlime(Entity<XenobiologyConsoleComponent> ent,
        ref XenobiologyConsoleSelectSlimeMessage args)
    {
        var (uid, console) = ent;
        if (!args.UiKey.Equals(XenobiologyConsoleUiKey.Key) ||
            !CanControl(uid, args.Actor) ||
            console.SelectedScanner is not { } scanner ||
            !GetLinkedScanners(uid).Contains(scanner) ||
            !_scannerQuery.TryComp(scanner, out var scannerComp) ||
            !TryGetEntity(args.Slime, out var slime) ||
            !_scanner.DetectSlimes((scanner, scannerComp)).Contains(slime.Value))
            return;

        console.SelectedSlime = slime;
        UpdateState(uid, console);
    }

    private void UpdateState(EntityUid uid, XenobiologyConsoleComponent console)
    {
        if (!_ui.IsUiOpen(uid, XenobiologyConsoleUiKey.Key))
            return;

        var linked = GetLinkedScanners(uid);
        if (!this.IsPowered(uid, EntityManager) ||
            console.SelectedScanner is not { } selectedScanner ||
            !linked.Contains(selectedScanner))
            ClearSelection(console);

        var scanners = linked.Select(linkedScanner =>
            new XenobiologyScannerEntry(GetNetEntity(linkedScanner), MetaData(linkedScanner).EntityName))
            .OrderBy(entry => entry.Entity).ToArray();
        var slimes = new List<XenobiologySlimeEntry>();
        SlimeScanData? scan = null;
        var interval = DefaultInterval;

        if (console.SelectedScanner is { } scanner && _scannerQuery.TryComp(scanner, out var scannerComp))
        {
            interval = TimeSpan.FromSeconds(Math.Max(scannerComp.UpdateInterval, 0.1f));
            var detected = _scanner.DetectSlimes((scanner, scannerComp));
            if (console.SelectedSlime is { } selected && !detected.Contains(selected))
                console.SelectedSlime = null;

            foreach (var slime in detected)
            {
                slimes.Add(new XenobiologySlimeEntry(GetNetEntity(slime), MetaData(slime).EntityName));
            }
            if (console.SelectedSlime is { } specimen)
                scan = _scan.TryBuildSlimeScanData(specimen);
        }

        slimes.Sort((a, b) => a.Entity.CompareTo(b.Entity));
        console.NextUpdate = _timing.CurTime + interval;
        var state = new XenobiologyConsoleUiState(scanners, GetNetEntity(console.SelectedScanner),
            [.. slimes], GetNetEntity(console.SelectedSlime), scan);
        if (SameState(console.LastState, state))
            return;

        console.LastState = state;
        _ui.SetUiState(uid, XenobiologyConsoleUiKey.Key, state);
    }

    private static void ClearSelection(XenobiologyConsoleComponent console)
    {
        console.SelectedScanner = null;
        console.SelectedSlime = null;
    }

    private static bool SameState(XenobiologyConsoleUiState? previous, XenobiologyConsoleUiState current)
    {
        if (previous == null ||
            previous.SelectedScanner != current.SelectedScanner ||
            previous.SelectedSlime != current.SelectedSlime ||
            !previous.LinkedScanners.SequenceEqual(current.LinkedScanners) ||
            !previous.DetectedSlimes.SequenceEqual(current.DetectedSlimes))
            return false;

        if (previous.Scan is not { } oldScan)
            return current.Scan == null;
        return current.Scan is { } newScan &&
               oldScan.TargetName == newScan.TargetName &&
               oldScan.Growth.Equals(newScan.Growth) &&
               Nullable.Equals(oldScan.Hunger, newScan.Hunger) &&
               oldScan.MutationChance.Equals(newScan.MutationChance) &&
               oldScan.ExtractYieldEnhanced == newScan.ExtractYieldEnhanced &&
               oldScan.Temperament == newScan.Temperament &&
               oldScan.Crowding == newScan.Crowding &&
               oldScan.PotentialMutations.SequenceEqual(newScan.PotentialMutations);
    }
}
