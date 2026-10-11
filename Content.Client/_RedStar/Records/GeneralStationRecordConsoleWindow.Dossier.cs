using Content.Client._RedStar.Records;
using Content.Shared._RedStar.Records;

namespace Content.Client.StationRecords;

public sealed partial class GeneralStationRecordConsoleWindow
{
    private CharacterDossier? _dossier;
    private uint? _dossierRecordId;

    private void AddDossierView()
    {
        if (_dossier != null && _dossierRecordId == _selectedDossierRecordId)
            RecordContainer.AddChild(new CharacterDossierView(_dossier, DossierSection.Employment));
    }

    private uint? _selectedDossierRecordId;

    private void UpdateDossierSelection(uint? recordId)
    {
        if (_selectedDossierRecordId == recordId)
            return;

        _selectedDossierRecordId = recordId;
        _dossier = null;
        _dossierRecordId = null;
    }

    public void UpdateDossier(uint? recordId, CharacterDossier? dossier)
    {
        if (recordId != _selectedDossierRecordId)
            return;

        _dossierRecordId = recordId;
        _dossier = dossier;

        if (RecordContainer.ChildCount == 0)
            return;

        if (RecordContainer.Children[RecordContainer.ChildCount - 1] is CharacterDossierView dossierView)
            dossierView.Orphan();

        AddDossierView();
    }
}
