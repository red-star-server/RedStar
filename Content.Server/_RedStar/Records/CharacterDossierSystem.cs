using System.Diagnostics.CodeAnalysis;
using Content.Shared._RedStar.Records;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Events;
using Content.Shared.StationRecords.Systems;

namespace Content.Server._RedStar.Records;

public sealed partial class CharacterDossierSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private StationRecordsSystem _stationRecords = default!;
    [Dependency] private EntityQuery<PdaComponent> _pdaQuery;
    [Dependency] private EntityQuery<StationRecordKeyStorageComponent> _recordKeyQuery;
    [Dependency] private EntityQuery<CharacterDossierComponent> _dossierQuery;

    [SubscribeLocalEvent(after: [typeof(StationRecordsSystem)])]
    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (!_inventory.TryGetSlotEntity(args.Mob, "id", out var id))
            return;

        var keyEntity = id.Value;
        if (_pdaQuery.TryComp(keyEntity, out var pda) && pda.ContainedId is { } containedId)
            keyEntity = containedId;

        if (!_recordKeyQuery.TryComp(keyEntity, out var storage) ||
            storage.Key is not { } key ||
            key.OriginStation != args.Station ||
            !_stationRecords.TryGetRecord<GeneralStationRecord>(key, out _))
            return;

        var component = EnsureComp<CharacterDossierComponent>(key.OriginStation);
        if (component.Records.HasRecordEntry<CharacterDossier>(key.Id))
            return;

        component.Records.AddRecordEntry(key.Id, args.Profile.Dossier.DeepCopy());
        RaiseLocalEvent(new CharacterDossierModifiedEvent(key));
    }

    [SubscribeLocalEvent]
    private void OnRecordRemoved(ref RecordRemovedEvent args)
    {
        if (!_dossierQuery.TryComp(args.Station, out var component) ||
            !component.Records.RemoveAllRecords(args.Key.Id))
            return;

        RaiseLocalEvent(new CharacterDossierModifiedEvent(args.Key));
    }

    public bool TryGet(StationRecordKey key, [NotNullWhen(true)] out CharacterDossier? dossier)
    {
        dossier = null;
        return _dossierQuery.TryComp(key.OriginStation, out var component) &&
               component.Records.TryGetRecordEntry(key.Id, out dossier);
    }
}

public sealed class CharacterDossierModifiedEvent(StationRecordKey key) : EntityEventArgs
{
    public StationRecordKey Key { get; } = key;
}
