using Content.Client._RedStar.Records;
using Content.Shared._RedStar.Records;

namespace Content.Client.CriminalRecords;

public sealed partial class CriminalRecordsConsoleWindow
{
    private void UpdateDossier(CharacterDossier? dossier)
    {
        DossierContainer.RemoveAllChildren();
        if (dossier != null)
            DossierContainer.AddChild(new CharacterDossierView(dossier, DossierSection.Security));
    }
}
