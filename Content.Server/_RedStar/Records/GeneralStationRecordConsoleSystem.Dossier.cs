using Content.Shared._RedStar.Records;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Server._RedStar.Records;
using Robust.Server.GameObjects;

namespace Content.Server.StationRecords;

public sealed partial class GeneralStationRecordConsoleSystem
{
    [Dependency] private CharacterDossierSystem _dossiers = default!;
    [Dependency] private UserInterfaceSystem _dossierUi = default!;

    public override void Initialize()
    {
        base.Initialize();
        Subs.BuiEvents<GeneralStationRecordConsoleComponent>(GeneralStationRecordConsoleKey.Key,
            events => events.Event<DossierRequestMessage>(OnDossierRequest));
    }

    private void OnDossierRequest(Entity<GeneralStationRecordConsoleComponent> ent, ref DossierRequestMessage args)
    {
        CharacterDossier? dossier = null;
        if (args.RecordId is { } id && StationSys.GetOwningStation(ent.Owner) is { } station)
        {
            var key = new StationRecordKey(id, station);
            if (_dossiers.TryGet(key, out var stored))
                dossier = stored.ForSection(ent.Comp.DossierSection);
        }

        _dossierUi.ServerSendUiMessage(ent.Owner,
            GeneralStationRecordConsoleKey.Key,
            new DossierStateMessage(dossier, ent.Comp.DossierSection),
            args.Actor);
    }
}
