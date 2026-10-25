using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;

namespace Content.Server._RedStar.Records;

/// <summary>
/// Resolves the record assigned by StationRecordsSystem to a newly spawned crew member.
/// </summary>
public sealed partial class SpawnStationRecordSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private EntityQuery<PdaComponent> _pdas;
    [Dependency] private EntityQuery<StationRecordKeyStorageComponent> _recordKeys;

    public bool TryGetKey(PlayerSpawnCompleteEvent args, out StationRecordKey key)
    {
        key = default;
        if (!_inventory.TryGetSlotEntity(args.Mob, "id", out var id))
            return false;

        var keyEntity = id.Value;
        if (_pdas.TryComp(keyEntity, out var pda) && pda.ContainedId is { } containedId)
            keyEntity = containedId;

        if (!_recordKeys.TryComp(keyEntity, out var storage) || storage.Key is not { } stored ||
            stored.OriginStation != args.Station ||
            !_records.TryGetRecord<GeneralStationRecord>(stored, out var record) ||
            record.Name != args.Profile.Name)
            return false;

        key = stored;
        return true;
    }
}
