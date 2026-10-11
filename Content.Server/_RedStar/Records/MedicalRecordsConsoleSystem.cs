using Content.Shared._RedStar.Records;
using Content.Shared.Access.Systems;
using Content.Shared.Station.Systems;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Events;
using Content.Shared.StationRecords.Systems;
using Robust.Server.GameObjects;

namespace Content.Server._RedStar.Records;

public sealed partial class MedicalRecordsConsoleSystem : EntitySystem
{
    [Dependency] private CharacterDossierSystem _dossiers = default!;
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private EntityQuery<StationRecordsComponent> _stationRecordsQuery;

    public override void Initialize()
    {
        base.Initialize();
        Subs.BuiEvents<MedicalRecordsConsoleComponent>(MedicalRecordsConsoleKey.Key, events =>
        {
            events.Event<BoundUIOpenedEvent>(OnOpened);
            events.Event<MedicalRecordSelectMessage>(OnSelected);
            events.Event<MedicalTriageSetPriorityMessage>(OnSetPriority);
            events.Event<MedicalTriageClaimMessage>(OnClaim);
        });
    }

    private void OnOpened(Entity<MedicalRecordsConsoleComponent> ent, ref BoundUIOpenedEvent args) => UpdateUi(ent);

    private void OnSelected(Entity<MedicalRecordsConsoleComponent> ent, ref MedicalRecordSelectMessage args)
    {
        ent.Comp.ActiveKey = args.Key;
        UpdateUi(ent);
    }

    /// <summary>
    /// Access is required for mutations, not reading, as on vanilla security consoles.
    /// </summary>
    private bool TryGetEditableRecord(Entity<MedicalRecordsConsoleComponent> ent, EntityUid actor,
        out EntityUid station, out uint id)
    {
        station = default;
        id = default;
        if (!_access.IsAllowed(actor, ent.Owner) ||
            _station.GetOwningStation(ent.Owner) is not { } owningStation ||
            ent.Comp.ActiveKey is not { } selected)
            return false;

        var key = new StationRecordKey(selected, owningStation);
        if (!_records.TryGetRecord<GeneralStationRecord>(key, out _))
            return false;

        station = owningStation;
        id = selected;
        return true;
    }

    private void OnSetPriority(Entity<MedicalRecordsConsoleComponent> ent, ref MedicalTriageSetPriorityMessage args)
    {
        if (!Enum.IsDefined(args.Priority) ||
            !TryGetEditableRecord(ent, args.Actor, out var station, out var id))
            return;

        var db = EnsureComp<MedicalTriageComponent>(station);
        if (!db.Entries.TryGetValue(id, out var entry))
            db.Entries[id] = entry = new MedicalTriageEntry();
        entry.Priority = args.Priority;
        UpdateUi(ent);
    }

    private void OnClaim(Entity<MedicalRecordsConsoleComponent> ent, ref MedicalTriageClaimMessage args)
    {
        if (!TryGetEditableRecord(ent, args.Actor, out var station, out var id))
            return;

        var db = EnsureComp<MedicalTriageComponent>(station);
        if (!db.Entries.TryGetValue(id, out var entry))
            db.Entries[id] = entry = new MedicalTriageEntry();

        if (entry.Doctor == args.Actor)
        {
            entry.Doctor = null;
            entry.DoctorName = null;
        }
        else
        {
            entry.Doctor = args.Actor;
            entry.DoctorName = MetaData(args.Actor).EntityName;
        }

        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnRecordModified(Entity<MedicalRecordsConsoleComponent> ent, ref RecordModifiedEvent args) => UpdateUi(ent);

    [SubscribeLocalEvent]
    private void OnGeneralRecordCreated(Entity<MedicalRecordsConsoleComponent> ent, ref GeneralRecordCreatedEvent args) => UpdateUi(ent);

    [SubscribeLocalEvent]
    private void OnRecordRemoved(Entity<MedicalRecordsConsoleComponent> ent, ref RecordRemovedEvent args)
    {
        if (TryComp<MedicalTriageComponent>(args.Station, out var triage))
            triage.Entries.Remove(args.Key.Id);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnDossierModified(Entity<MedicalRecordsConsoleComponent> ent, ref CharacterDossierModifiedEvent args)
    {
        if (_station.GetOwningStation(ent.Owner) == args.Key.OriginStation)
            UpdateUi(ent);
    }

    private void UpdateUi(Entity<MedicalRecordsConsoleComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, MedicalRecordsConsoleKey.Key))
            return;

        if (_station.GetOwningStation(ent.Owner) is not { } station ||
            !_stationRecordsQuery.TryComp(station, out var stationRecords))
        {
            _ui.SetUiState(ent.Owner, MedicalRecordsConsoleKey.Key,
                new MedicalRecordsConsoleState([], null, null, null));
            return;
        }

        var listing = _records.BuildListing((station, stationRecords), null);
        GeneralStationRecord? general = null;
        CharacterDossier? dossier = null;
        MedicalTriageSnapshot? triageSnapshot = null;
        if (ent.Comp.ActiveKey is { } id)
        {
            var key = new StationRecordKey(id, station);
            _records.TryGetRecord(key, out general, stationRecords);
            if (general != null)
            {
                if (_dossiers.TryGet(key, out var stored))
                    dossier = stored.ForSection(DossierSection.Medical);
                if (TryComp<MedicalTriageComponent>(station, out var db) &&
                    db.Entries.TryGetValue(id, out var triage))
                    triageSnapshot = new MedicalTriageSnapshot(triage.Priority, triage.DoctorName);
            }
        }

        _ui.SetUiState(ent.Owner, MedicalRecordsConsoleKey.Key,
            new MedicalRecordsConsoleState(listing, ent.Comp.ActiveKey, general, dossier, triageSnapshot));
    }
}
