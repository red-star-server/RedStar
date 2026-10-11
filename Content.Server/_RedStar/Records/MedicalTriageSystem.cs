using Content.Shared._RedStar.Records;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Events;
using Content.Shared.StationRecords.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Records;

/// <summary>
/// Stores round triage and keeps the current patient's HUD in sync.
/// </summary>
public sealed partial class MedicalTriageSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private EntityQuery<PdaComponent> _pdas;
    [Dependency] private EntityQuery<StationRecordKeyStorageComponent> _recordKeys;

    private readonly Dictionary<StationRecordKey, EntityUid> _patients = new();
    private static readonly ProtoId<AccessLevelPrototype> MedicalAccess = "Medical";

    public bool HasMedicalAccess(EntityUid actor) => _access.FindAccessTags(actor).Contains(MedicalAccess);

    public bool TryGetPatientKey(EntityUid patient, out StationRecordKey key)
    {
        key = default;
        if (Deleted(patient) ||
            !TryComp<MedicalTriagePatientComponent>(patient, out var binding) ||
            !_patients.TryGetValue(binding.Key, out var current) || current != patient ||
            !_records.TryGetRecord<GeneralStationRecord>(binding.Key, out _))
            return false;

        key = binding.Key;
        return true;
    }

    public MedicalTriageSnapshot? Get(StationRecordKey key)
    {
        if (!_records.TryGetRecord<GeneralStationRecord>(key, out _) ||
            !TryComp<MedicalTriageComponent>(key.OriginStation, out var db) ||
            !db.Entries.TryGetValue(key.Id, out var priority))
            return null;

        return new MedicalTriageSnapshot(priority);
    }

    public bool SetPriority(StationRecordKey key, MedicalTriagePriority priority, EntityUid actor)
    {
        if (!HasMedicalAccess(actor) || !Enum.IsDefined(priority) ||
            !_records.TryGetRecord<GeneralStationRecord>(key, out _))
            return false;

        var db = EnsureComp<MedicalTriageComponent>(key.OriginStation);
        if (priority == MedicalTriagePriority.None)
            db.Entries.Remove(key.Id);
        else
            db.Entries[key.Id] = priority;

        UpdatePatient(key, priority);
        RaiseLocalEvent(new MedicalTriageChangedEvent());
        return true;
    }

    private void UpdatePatient(StationRecordKey key, MedicalTriagePriority priority)
    {
        if (!_patients.TryGetValue(key, out var patient) || Deleted(patient))
            return;

        if (priority == MedicalTriagePriority.None)
        {
            RemComp<MedicalTriageStatusComponent>(patient);
            return;
        }

        var status = EnsureComp<MedicalTriageStatusComponent>(patient);
        status.Priority = priority;
        Dirty(patient, status);
    }

    [SubscribeLocalEvent(after: [typeof(StationRecordsSystem)])]
    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (!_inventory.TryGetSlotEntity(args.Mob, "id", out var idEntity))
            return;

        var keyEntity = idEntity.Value;
        if (_pdas.TryComp(keyEntity, out var pda) && pda.ContainedId is { } containedId)
            keyEntity = containedId;

        if (!_recordKeys.TryComp(keyEntity, out var storage) || storage.Key is not { } key ||
            key.OriginStation != args.Station ||
            !_records.TryGetRecord<GeneralStationRecord>(key, out var record) ||
            record.Name != args.Profile.Name)
            return;

        if (_patients.TryGetValue(key, out var oldPatient) && oldPatient != args.Mob && !Deleted(oldPatient))
        {
            RemComp<MedicalTriagePatientComponent>(oldPatient);
            RemComp<MedicalTriageStatusComponent>(oldPatient);
        }

        var binding = EnsureComp<MedicalTriagePatientComponent>(args.Mob);
        binding.Key = key;
        _patients[key] = args.Mob;
        var priority = TryComp<MedicalTriageComponent>(args.Station, out var db) &&
                       db.Entries.TryGetValue(key.Id, out var stored)
            ? stored
            : MedicalTriagePriority.None;
        UpdatePatient(key, priority);
    }

    [SubscribeLocalEvent]
    private void OnPatientShutdown(Entity<MedicalTriagePatientComponent> ent, ref ComponentShutdown args)
    {
        if (_patients.TryGetValue(ent.Comp.Key, out var patient) && patient == ent.Owner)
            _patients.Remove(ent.Comp.Key);
    }

    [SubscribeLocalEvent]
    private void OnRecordRemoved(ref RecordRemovedEvent args)
    {
        if (TryComp<MedicalTriageComponent>(args.Station, out var db))
            db.Entries.Remove(args.Key.Id);

        if (_patients.Remove(args.Key, out var patient) && !Deleted(patient))
        {
            RemComp<MedicalTriagePatientComponent>(patient);
            RemComp<MedicalTriageStatusComponent>(patient);
        }

        RaiseLocalEvent(new MedicalTriageChangedEvent());
    }
}

public sealed class MedicalTriageChangedEvent : EntityEventArgs;
