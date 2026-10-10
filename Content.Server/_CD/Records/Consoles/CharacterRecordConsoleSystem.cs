using Content.Server.CriminalRecords.Systems;
using Content.Shared.StationRecords.Systems;
using Content.Shared.Station.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.CriminalRecords.Components;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Content.Shared._CD.Records;
using Content.Shared.StationRecords.Components;
using Robust.Server.GameObjects;

namespace Content.Server._CD.Records.Consoles;

public sealed partial class CharacterRecordConsoleSystem : EntitySystem
{
    [Dependency] private CharacterRecordsSystem _characterRecords = default!;
    [Dependency] private CriminalRecordsConsoleSystem _criminalRecordsConsole = default!;
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<CharacterRecordConsoleComponent>(CharacterRecordConsoleKey.Key,
            subr =>
            {
                subr.Event<BoundUIOpenedEvent>((uid, component, _) => UpdateUi((uid, component)));
                subr.Event<CharacterRecordConsoleSelectMsg>(OnKeySelect);
                subr.Event<CharacterRecordsConsoleFilterMsg>(OnFilterApplied);
                subr.Event<CriminalRecordChangeStatus>(OnCriminalRecordChangeStatus);
            });
    }

    [SubscribeLocalEvent]
    private void OnRecordsModified(CharacterRecordsModifiedEvent args)
    {
        var query = EntityQueryEnumerator<CharacterRecordConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            if (console.ConsoleType == RecordConsoleType.Admin || _station.GetOwningStation(uid) == args.Station)
                UpdateUi((uid, console));
        }
    }

    private void OnFilterApplied(Entity<CharacterRecordConsoleComponent> ent, ref CharacterRecordsConsoleFilterMsg msg)
    {
        ent.Comp.Filter = msg.Filter;
        UpdateUi(ent);
    }

    private void OnKeySelect(Entity<CharacterRecordConsoleComponent> ent, ref CharacterRecordConsoleSelectMsg msg)
    {
        ent.Comp.SelectedIndex = msg.CharacterRecordKey;
        if (TryComp<CriminalRecordsConsoleComponent>(ent, out var criminalConsole) &&
            _station.GetOwningStation(ent) is { } station)
        {
            uint? stationKey = null;
            if (msg.CharacterRecordKey is { } characterKey &&
                _characterRecords.QueryRecords(station).TryGetValue(characterKey, out var record))
                stationKey = record.StationRecordsKey;

            _criminalRecordsConsole.SelectRecord((ent.Owner, criminalConsole), stationKey);
        }
        UpdateUi(ent);
    }

    private void OnCriminalRecordChangeStatus(Entity<CharacterRecordConsoleComponent> ent, ref CriminalRecordChangeStatus msg)
    {
        if (!TryComp<CriminalRecordsConsoleComponent>(ent, out var console))
            return;

        _criminalRecordsConsole.OnChangeStatus((ent.Owner, console), ref msg);
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<CharacterRecordConsoleComponent> ent)
    {
        var entity = ent.Owner;
        var console = ent.Comp;
        var station = _station.GetOwningStation(entity);
        if (station == null && console.ConsoleType == RecordConsoleType.Admin)
        {
            foreach (var candidate in _station.GetStations())
            {
                if (!HasComp<CharacterRecordsComponent>(candidate))
                    continue;

                station = candidate;
                break;
            }
        }
        if (!HasComp<StationRecordsComponent>(station) || !HasComp<CharacterRecordsComponent>(station))
        {
            SendState(entity, new CharacterRecordConsoleState { ConsoleType = console.ConsoleType });
            return;
        }

        var characterRecords = _characterRecords.QueryRecords(station.Value);
        // Get the names to display from the list of records.
        var names = new Dictionary<uint, string>();
        foreach (var (i, r) in characterRecords)
        {
            var netEnt = r.Owner is { } owner ? _entity.GetNetEntity(owner).ToString() : "unknown";
            // Admins get additional info to make it easier to run commands
            var nameJob = console.ConsoleType != RecordConsoleType.Admin
                ? $"{r.Name} ({r.JobTitle})"
                : $"{r.Name} ({netEnt}, {r.JobTitle})";

            // Apply any filter the user has set
            if (console.Filter != null)
            {
                if (IsSkippedRecord(console.Filter, r))
                    continue;
            }

            names[i] = nameJob;
        }

        var record =
            console.SelectedIndex == null || !characterRecords.TryGetValue(console.SelectedIndex!.Value, out var value)
                ? null
                : value;
        (SecurityStatus, string?)? securityStatus = null;

        // If we need the character's security status, gather it from the criminal records
        if ((console.ConsoleType == RecordConsoleType.Admin ||
             console.ConsoleType == RecordConsoleType.Security)
            && record?.StationRecordsKey != null)
        {
            var key = new StationRecordKey(record.StationRecordsKey.Value, station.Value);
            if (_records.TryGetRecord<CriminalRecord>(key, out var entry))
                securityStatus = (entry.Status, entry.Reason);
        }

        SendState(entity,
            new CharacterRecordConsoleState
            {
                ConsoleType = console.ConsoleType,
                CharacterList = names,
                SelectedIndex = console.SelectedIndex,
                SelectedRecord = record,
                Filter = console.Filter,
                SelectedSecurityStatus = securityStatus,
            });
    }

    private void SendState(EntityUid entity, CharacterRecordConsoleState state)
    {
        _ui.SetUiState(entity, CharacterRecordConsoleKey.Key, state);
    }

    /// <summary>
    /// Almost exactly the same as <see cref="StationRecordsSystem.IsSkipped"/>
    /// </summary>
    private static bool IsSkippedRecord(StationRecordsFilter filter,
        FullCharacterRecords record)
    {
        if (filter.Value.Length == 0)
            return false;

        return filter.Type switch
        {
            StationRecordFilterType.Name =>
                !record.Name.Contains(filter.Value, StringComparison.OrdinalIgnoreCase),
            StationRecordFilterType.Job =>
                !record.JobTitle.Contains(filter.Value, StringComparison.OrdinalIgnoreCase),
            StationRecordFilterType.Species =>
                !record.Species.Contains(filter.Value, StringComparison.OrdinalIgnoreCase),
            StationRecordFilterType.Prints => record.Fingerprint == null
                || !record.Fingerprint.StartsWith(filter.Value, StringComparison.OrdinalIgnoreCase),
            StationRecordFilterType.DNA => record.DNA == null
                || !record.DNA.StartsWith(filter.Value, StringComparison.OrdinalIgnoreCase),
            _ => true,
        };
    }
}
