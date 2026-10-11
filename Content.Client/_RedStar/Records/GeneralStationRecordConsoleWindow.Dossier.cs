using Content.Client._RedStar.Records;
using Content.Shared._RedStar.Records;

namespace Content.Client.StationRecords;

public sealed partial class GeneralStationRecordConsoleWindow
{
    private CharacterDossier? _dossier;

    private void AddDossierView()
    {
        if (_dossier != null)
            RecordContainer.AddChild(new CharacterDossierView(_dossier, DossierSection.Employment));
    }

    public void UpdateDossier(CharacterDossier? dossier)
    {
        _dossier = dossier;

        if (RecordContainer.ChildCount == 0)
            return;

        if (RecordContainer.Children[RecordContainer.ChildCount - 1] is CharacterDossierView dossierView)
            dossierView.Orphan();

        AddDossierView();
    }
}
