using Content.Server._RedStar.Records;
using Content.Shared._RedStar.Records;
using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;

namespace Content.Server.CriminalRecords.Systems;

public sealed partial class CriminalRecordsConsoleSystem
{
    [Dependency] private CharacterDossierSystem _dossiers = default!;

    private void PopulateDossier(StationRecordKey key, CriminalRecordsConsoleState state)
    {
        if (_dossiers.TryGet(key, out var dossier) && dossier != null)
            state.Dossier = dossier.ForSection(DossierSection.Security);
    }
}
