using Content.Server.CriminalRecords.Systems;
using Content.Shared.StationRecords.Systems;
using Content.Shared.Station.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.CriminalRecords.Components;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Content.Shared._CD.Records;
using Content.Shared.StationRecords.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Verbs;
using Content.Shared.Humanoid;
using Content.Server.Administration.Managers;
using Robust.Server.GameObjects;

namespace Content.Server._CD.Records.Consoles;

public sealed partial class CharacterRecordConsoleSystem : EntitySystem
{
    [Dependency] private CriminalRecordsConsoleSystem _criminalRecordsConsole = default!;
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private CharacterRecordsSystem _characterRecords = default!;
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<CharacterRecordConsoleComponent>(CharacterRecordConsoleKey.Key,
            subr =>
            {
                subr.Event<BoundUIOpenedEvent>(OnUiOpened);
                subr.Event<CharacterRecordConsoleSelectMsg>(OnKeySelect);
                subr.Event<CharacterRecordsConsoleFilterMsg>(OnFilterApplied);
                subr.Event<CriminalRecordChangeStatus>(OnCriminalRecordChangeStatus);
            });
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<CharacterRecordConsoleComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (ent.Comp.ConsoleType == RecordConsoleType.Admin || !CanView(ent, args.User))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rs-character-records-open-rp-records"),
            Act = () => _ui.TryOpenUi(ent.Owner, CharacterRecordConsoleKey.Key, user),
        });
    }

    private bool CanView(Entity<CharacterRecordConsoleComponent> ent, EntityUid user)
    {
        return ent.Comp.ConsoleType == RecordConsoleType.Admin
            ? _admins.IsAdmin(user)
            : _access.IsAllowed(user, ent.Owner);
    }

    private void OnUiOpened(Entity<CharacterRecordConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!CanView(ent, args.Actor))
        {
            _ui.CloseUi(ent.Owner, CharacterRecordConsoleKey.Key, args.Actor);
            return;
        }

        UpdateUi(ent);
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
        if (!CanView(ent, msg.Actor))
            return;
        if (msg.Filter is { } filter)
        {
            if (filter.Value == null || filter.Value.Length > PlayerProvidedCharacterRecords.TextMedLen ||
                (ent.Comp.ConsoleType is RecordConsoleType.Employment or RecordConsoleType.Medical) &&
                filter.Type != StationRecordFilterType.Name)
                return;
        }
        ent.Comp.Filter = msg.Filter;
        UpdateUi(ent);
    }

    private void OnKeySelect(Entity<CharacterRecordConsoleComponent> ent, ref CharacterRecordConsoleSelectMsg msg)
    {
        if (!CanView(ent, msg.Actor))
            return;
        ent.Comp.SelectedIndex = msg.CharacterRecordKey;
        if (TryComp<CriminalRecordsConsoleComponent>(ent, out var criminalConsole) &&
            _station.GetOwningStation(ent) is { } station)
        {
            _criminalRecordsConsole.SelectRecord((ent.Owner, criminalConsole), msg.CharacterRecordKey);
        }
        UpdateUi(ent);
    }

    private void OnCriminalRecordChangeStatus(Entity<CharacterRecordConsoleComponent> ent, ref CriminalRecordChangeStatus msg)
    {
        if (!CanView(ent, msg.Actor))
            return;
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
                if (!HasComp<StationRecordsComponent>(candidate))
                    continue;

                station = candidate;
                break;
            }
        }
        if (!HasComp<StationRecordsComponent>(station))
        {
            SendState(entity, new CharacterRecordConsoleState { ConsoleType = console.ConsoleType });
            return;
        }

        var characterRecords = _characterRecords.QueryRecords(station.Value);
        // Get the names to display from the list of records.
        var names = new Dictionary<uint, string>();
        foreach (var (i, r) in characterRecords)
        {
            if (!_records.TryGetRecord<GeneralStationRecord>(new StationRecordKey(i, station.Value), out var general))
                continue;

            var netEnt = Exists(r.Owner) ? _entity.GetNetEntity(r.Owner).ToString() : "unknown";
            // Admins get additional info to make it easier to run commands
            var nameJob = console.ConsoleType != RecordConsoleType.Admin
                ? $"{general.Name} ({general.JobTitle})"
                : $"{general.Name} ({netEnt}, {general.JobTitle})";

            // Apply any filter the user has set
            if (console.Filter != null)
            {
                if (IsSkippedRecord(console.Filter, general))
                    continue;
            }

            names[i] = nameJob;
        }

        GeneralStationRecord? selectedGeneral = null;
        PlayerProvidedCharacterRecords? selectedDetails = null;
        Sex? selectedSex = null;
        if (console.SelectedIndex is { } selected &&
            characterRecords.TryGetValue(selected, out var rpRecord) &&
            _records.TryGetRecord<GeneralStationRecord>(new StationRecordKey(selected, station.Value), out var generalRecord))
        {
            selectedDetails = LimitToConsole(new PlayerProvidedCharacterRecords(rpRecord.Details), console.ConsoleType);
            selectedGeneral = console.ConsoleType is RecordConsoleType.Security or RecordConsoleType.Admin
                ? generalRecord with { }
                : generalRecord with { Fingerprint = null, DNA = null };
            if (console.ConsoleType is RecordConsoleType.Medical or RecordConsoleType.Admin)
                selectedSex = rpRecord.Sex;
        }
        (SecurityStatus, string?)? securityStatus = null;

        // If we need the character's security status, gather it from the criminal records
        if ((console.ConsoleType == RecordConsoleType.Admin ||
             console.ConsoleType == RecordConsoleType.Security)
            && console.SelectedIndex is { } selectedKey)
        {
            var key = new StationRecordKey(selectedKey, station.Value);
            if (_records.TryGetRecord<CriminalRecord>(key, out var entry))
                securityStatus = (entry.Status, entry.Reason);
        }

        SendState(entity,
            new CharacterRecordConsoleState
            {
                ConsoleType = console.ConsoleType,
                CharacterList = names,
                SelectedIndex = console.SelectedIndex,
                SelectedGeneralRecord = selectedGeneral,
                SelectedDetails = selectedDetails,
                SelectedSex = selectedSex,
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
        GeneralStationRecord record)
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

    private static PlayerProvidedCharacterRecords LimitToConsole(PlayerProvidedCharacterRecords details, RecordConsoleType type)
    {
        // The UI hides other sections, but the network state must not carry them either.
        if (type is not (RecordConsoleType.Medical or RecordConsoleType.Admin))
            details = details.WithWeight(0);

        if (type is RecordConsoleType.Security)
            details = details.WithContactName(string.Empty);

        if (type is not (RecordConsoleType.Employment or RecordConsoleType.Admin))
        {
            details = details.WithEmploymentEntries([]).WithWorkAuth(false);
        }

        if (type is not (RecordConsoleType.Medical or RecordConsoleType.Admin))
        {
            details = details.WithMedicalEntries([]).WithAllergies(string.Empty)
                .WithDrugAllergies(string.Empty).WithPostmortemInstructions(string.Empty);
        }

        if (type is not (RecordConsoleType.Security or RecordConsoleType.Admin))
        {
            details = details.WithSecurityEntries([]).WithIdentifyingFeatures(string.Empty);
        }
        return details;
    }
}
