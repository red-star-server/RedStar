using Content.Shared.StationRecords.Systems;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Content.Shared._CD.Records;
using Content.Shared.Forensics.Components;
using Content.Shared.GameTicking;
using Content.Shared.StationRecords.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._CD.Records;

public sealed partial class CharacterRecordsSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private StationRecordsSystem _records = default!;

    [SubscribeLocalEvent(after: [typeof(StationRecordsSystem)])]
    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (!HasComp<StationRecordsComponent>(args.Station))
            return;

        if (string.IsNullOrEmpty(args.JobId))
            return;

        var profile = args.Profile;
        if (profile.CDCharacterRecords == null)
            return;

        var player = args.Mob;

        if (!_prototype.TryIndex(args.JobId, out JobPrototype? jobPrototype))
            return;

        EnsureComp<CharacterRecordsComponent>(args.Station);

        TryComp<FingerprintComponent>(player, out var fingerprintComponent);
        TryComp<DnaComponent>(player, out var dnaComponent);

        var jobTitle = jobPrototype.LocalizedName;
        var stationRecordsKey = FindStationRecordsKey(player);

        // Grab the title from the station records if they exist to support our job title system
        if (stationRecordsKey != null && _records.TryGetRecord<GeneralStationRecord>(stationRecordsKey.Value, out var stationRecords))
        {
            jobTitle = stationRecords.JobTitle;
        }

        var records = new FullCharacterRecords(
            pRecords: new PlayerProvidedCharacterRecords(profile.CDCharacterRecords),
            stationRecordsKey: stationRecordsKey?.Id,
            name: profile.Name,
            age: profile.Age,
            species: profile.Species,
            jobTitle: jobTitle,
            jobIcon: jobPrototype.Icon,
            gender: profile.Gender,
            sex: profile.Sex,
            fingerprint: fingerprintComponent?.Fingerprint,
            dna: dnaComponent?.DNA,
            owner: player);
        AddRecord(args.Station, args.Mob, records);
    }

    private StationRecordKey? FindStationRecordsKey(EntityUid uid)
    {
        if (!_inventory.TryGetSlotEntity(uid, "id", out var idUid))
            return null;

        var keyStorageEntity = idUid;
        if (TryComp<PdaComponent>(idUid, out var pda) && pda.ContainedId is {} id)
        {
            keyStorageEntity = id;
        }

        return !TryComp<StationRecordKeyStorageComponent>(keyStorageEntity, out var storage) ? null : storage.Key;
    }

    private void AddRecord(EntityUid station, EntityUid player, FullCharacterRecords records, CharacterRecordsComponent? recordsDb = null)
    {
        if (!Resolve(station, ref recordsDb))
            return;

        var key = recordsDb.CreateNewKey();
        recordsDb.Records.Add(key, records);
        var playerKey = new CharacterRecordKey { Station = station, Index = key };
        AddComp(player, new CharacterRecordKeyStorageComponent(playerKey));

        RaiseLocalEvent(new CharacterRecordsModifiedEvent(station));
    }

    public void DelEntry(
        EntityUid player,
        CharacterRecordType ty,
        int idx,
        CharacterRecordKeyStorageComponent? key = null)
    {
        if (!Resolve(player, ref key) ||
            !TryComp<CharacterRecordsComponent>(key.Key.Station, out var recordsDb))
            return;

        if (!recordsDb.Records.TryGetValue(key.Key.Index, out var value))
            return;

        var cr = value.PRecords;

        var entries = ty switch
        {
            CharacterRecordType.Employment => cr.EmploymentEntries,
            CharacterRecordType.Medical => cr.MedicalEntries,
            CharacterRecordType.Security => cr.SecurityEntries,
            _ => null,
        };
        if (entries is null || idx < 0 || idx >= entries.Count)
            return;

        entries.RemoveAt(idx);

        RaiseLocalEvent(new CharacterRecordsModifiedEvent(key.Key.Station));
    }

    public void ResetRecord(
        EntityUid player,
        CharacterRecordKeyStorageComponent? key = null)
    {
        if (!Resolve(player, ref key) ||
            !TryComp<CharacterRecordsComponent>(key.Key.Station, out var recordsDb))
            return;

        if (!recordsDb.Records.TryGetValue(key.Key.Index, out var value))
            return;

        var records = PlayerProvidedCharacterRecords.DefaultRecords();
        if (TryComp(player, out MetaDataComponent? meta))
            value.Name = meta.EntityName;
        value.PRecords = records;
        RaiseLocalEvent(new CharacterRecordsModifiedEvent(key.Key.Station));
    }

    public IDictionary<uint, FullCharacterRecords> QueryRecords(EntityUid station, CharacterRecordsComponent? recordsDb = null)
    {
        return !Resolve(station, ref recordsDb)
            ? new Dictionary<uint, FullCharacterRecords>()
            : recordsDb.Records;
    }
}

public sealed class CharacterRecordsModifiedEvent(EntityUid station) : EntityEventArgs
{
    public EntityUid Station { get; } = station;
}
