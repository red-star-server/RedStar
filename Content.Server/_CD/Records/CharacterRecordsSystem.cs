using Content.Shared._CD.Records;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Events;
using Content.Shared.StationRecords.Systems;

namespace Content.Server._CD.Records;

public sealed partial class CharacterRecordsSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private StationRecordsSystem _records = default!;

    [SubscribeLocalEvent(after: [typeof(StationRecordsSystem)])]
    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (args.Profile.CDCharacterRecords is not { } details ||
            FindStationRecordsKey(args.Mob) is not { } key ||
            key.OriginStation != args.Station ||
            !_records.TryGetRecord<GeneralStationRecord>(key, out _))
            return;

        // A reused general record keeps the same key across respawns.
        var db = EnsureComp<CharacterRecordsComponent>(args.Station);
        db.Records[key.Id] = (new PlayerProvidedCharacterRecords(details), args.Profile.Sex, args.Mob);
        RaiseLocalEvent(new CharacterRecordsModifiedEvent(key.OriginStation));
    }

    [SubscribeLocalEvent]
    private void OnRecordRemoved(ref RecordRemovedEvent args)
    {
        if (TryComp<CharacterRecordsComponent>(args.Station, out var db) && db.Records.Remove(args.Key.Id))
            RaiseLocalEvent(new CharacterRecordsModifiedEvent(args.Station));
    }

    private StationRecordKey? FindStationRecordsKey(EntityUid player)
    {
        if (!_inventory.TryGetSlotEntity(player, "id", out var idUid))
            return null;

        var storageUid = idUid.Value;
        if (TryComp<PdaComponent>(storageUid, out var pda) && pda.ContainedId is { } containedId)
            storageUid = containedId;

        return TryComp<StationRecordKeyStorageComponent>(storageUid, out var storage)
            ? storage.Key
            : null;
    }

    public void DelEntry(EntityUid player, CharacterRecordType type, int index)
    {
        if (FindStationRecordsKey(player) is not { } key ||
            !TryComp<CharacterRecordsComponent>(key.OriginStation, out var db) ||
            !db.Records.TryGetValue(key.Id, out var record))
            return;

        var entries = type switch
        {
            CharacterRecordType.Employment => record.Details.EmploymentEntries,
            CharacterRecordType.Medical => record.Details.MedicalEntries,
            CharacterRecordType.Security => record.Details.SecurityEntries,
            _ => null,
        };

        if (entries == null || index < 0 || index >= entries.Count)
            return;

        entries.RemoveAt(index);
        RaiseLocalEvent(new CharacterRecordsModifiedEvent(key.OriginStation));
    }

    public void ResetRecord(EntityUid player)
    {
        if (FindStationRecordsKey(player) is not { } key ||
            !TryComp<CharacterRecordsComponent>(key.OriginStation, out var db) ||
            !db.Records.TryGetValue(key.Id, out var record))
            return;

        db.Records[key.Id] = (PlayerProvidedCharacterRecords.DefaultRecords(), record.Sex, record.Owner);
        RaiseLocalEvent(new CharacterRecordsModifiedEvent(key.OriginStation));
    }

    public IReadOnlyDictionary<uint, (PlayerProvidedCharacterRecords Details, Content.Shared.Humanoid.Sex Sex, EntityUid Owner)>
        QueryRecords(EntityUid station)
    {
        return TryComp<CharacterRecordsComponent>(station, out var db)
            ? db.Records
            : new Dictionary<uint, (PlayerProvidedCharacterRecords, Content.Shared.Humanoid.Sex, EntityUid)>();
    }
}

public sealed class CharacterRecordsModifiedEvent(EntityUid station) : EntityEventArgs
{
    public EntityUid Station { get; } = station;
}
