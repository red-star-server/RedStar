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
    [Dependency] private MedicalTriageSystem _triage = default!;
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
        });
    }

    private void OnOpened(Entity<MedicalRecordsConsoleComponent> ent, ref BoundUIOpenedEvent args) => UpdateUi(ent);

    private void OnSelected(Entity<MedicalRecordsConsoleComponent> ent, ref MedicalRecordSelectMessage args)
    {
        ent.Comp.ActiveKey = args.Key;
        UpdateUi(ent);
    }

    private bool TryGetEditableKey(Entity<MedicalRecordsConsoleComponent> ent, EntityUid actor,
        out StationRecordKey key)
    {
        key = default;
        if (!_access.IsAllowed(actor, ent.Owner) ||
            !_triage.HasMedicalAccess(actor) ||
            _station.GetOwningStation(ent.Owner) is not { } station ||
            ent.Comp.ActiveKey is not { } id)
            return false;

        key = new StationRecordKey(id, station);
        return true;
    }

    private void OnSetPriority(Entity<MedicalRecordsConsoleComponent> ent, ref MedicalTriageSetPriorityMessage args)
    {
        if (TryGetEditableKey(ent, args.Actor, out var key))
            _triage.SetPriority(key, args.Priority, args.Actor);
    }

    [SubscribeLocalEvent]
    private void OnRecordModified(ref RecordModifiedEvent args) => UpdateOpenConsoles();

    [SubscribeLocalEvent]
    private void OnGeneralRecordCreated(ref GeneralRecordCreatedEvent args) => UpdateOpenConsoles();

    [SubscribeLocalEvent]
    private void OnRecordRemoved(ref RecordRemovedEvent args) => UpdateOpenConsoles();

    [SubscribeLocalEvent]
    private void OnDossierModified(ref CharacterDossierModifiedEvent args) => UpdateOpenConsoles();

    [SubscribeLocalEvent]
    private void OnTriageChanged(MedicalTriageChangedEvent args) => UpdateOpenConsoles();

    private void UpdateOpenConsoles()
    {
        var query = EntityQueryEnumerator<MedicalRecordsConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateUi((uid, comp));
        }
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
                triageSnapshot = _triage.Get(key);
            }
        }

        _ui.SetUiState(ent.Owner, MedicalRecordsConsoleKey.Key,
            new MedicalRecordsConsoleState(listing, ent.Comp.ActiveKey, general, dossier, triageSnapshot));
    }
}
