using Content.Client._RedStar.Records;
using Content.Shared._RedStar.Records;

namespace Content.Client.StationRecords;

public sealed partial class GeneralStationRecordConsoleWindow
{
    private CharacterDossier? _dossier;
    private DossierSection _dossierSection;

    private void AddDossierView()
    {
        if (_dossier != null)
            RecordContainer.AddChild(new CharacterDossierView(_dossier, _dossierSection));
    }

    public void UpdateDossier(CharacterDossier? dossier, DossierSection section)
    {
        _dossier = dossier;
        _dossierSection = section;

        if (RecordContainer.ChildCount == 0)
            return;

        if (RecordContainer.Children[RecordContainer.ChildCount - 1] is CharacterDossierView dossierView)
            dossierView.Orphan();

        AddDossierView();
    }
}
