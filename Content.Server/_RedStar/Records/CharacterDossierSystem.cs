using System.Diagnostics.CodeAnalysis;
using Content.Shared._RedStar.Records;
using Content.Shared.GameTicking;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Events;
using Content.Shared.StationRecords.Systems;

namespace Content.Server._RedStar.Records;

public sealed partial class CharacterDossierSystem : EntitySystem
{
    [Dependency] private SpawnStationRecordSystem _spawnRecords = default!;
    [Dependency] private EntityQuery<CharacterDossierComponent> _dossierQuery;

    [SubscribeLocalEvent(after: [typeof(StationRecordsSystem)])]
    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (!_spawnRecords.TryGetKey(args, out var key))
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
