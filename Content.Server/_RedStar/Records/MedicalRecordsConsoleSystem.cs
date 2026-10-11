using Content.Shared._RedStar.Records;
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
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private EntityQuery<StationRecordsComponent> _stationRecordsQuery;

    public override void Initialize()
    {
        base.Initialize();
        Subs.BuiEvents<MedicalRecordsConsoleComponent>(MedicalRecordsConsoleKey.Key, events =>
        {
            events.Event<BoundUIOpenedEvent>(OnOpened);
            events.Event<MedicalRecordSelectMessage>(OnSelected);
        });
    }

    private void OnOpened(Entity<MedicalRecordsConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnSelected(Entity<MedicalRecordsConsoleComponent> ent, ref MedicalRecordSelectMessage args)
    {
        ent.Comp.ActiveKey = args.Key;
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnRecordModified(Entity<MedicalRecordsConsoleComponent> ent, ref RecordModifiedEvent args) => UpdateUi(ent);

    [SubscribeLocalEvent]
    private void OnGeneralRecordCreated(Entity<MedicalRecordsConsoleComponent> ent, ref GeneralRecordCreatedEvent args) => UpdateUi(ent);

    [SubscribeLocalEvent]
    private void OnRecordRemoved(Entity<MedicalRecordsConsoleComponent> ent, ref RecordRemovedEvent args) => UpdateUi(ent);

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
        if (ent.Comp.ActiveKey is { } id)
        {
            var key = new StationRecordKey(id, station);
            _records.TryGetRecord(key, out general, stationRecords);
            if (_dossiers.TryGet(key, out var stored))
                dossier = stored.ForSection(DossierSection.Medical);
        }

        _ui.SetUiState(ent.Owner, MedicalRecordsConsoleKey.Key,
            new MedicalRecordsConsoleState(listing, ent.Comp.ActiveKey, general, dossier));
    }
}
