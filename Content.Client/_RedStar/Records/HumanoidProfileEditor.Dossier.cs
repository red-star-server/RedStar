using Content.Client._CD.Records.UI;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private RecordEditorGui _dossierEditor = default!;

    private void InitializeDossierEditor()
    {
        _dossierEditor = new RecordEditorGui(dossier =>
        {
            if (Profile == null)
                return;

            Profile = Profile.WithDossier(dossier);
            SetDirty();
        });
        TabContainer.AddChild(_dossierEditor);
        TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("character-dossier-tab-title"));
    }

    private void UpdateDossierEditor()
    {
        _dossierEditor.Update(Profile);
    }
}
