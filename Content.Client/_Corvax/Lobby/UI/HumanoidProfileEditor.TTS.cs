using Content.Client._Corvax.TTS;
using Content.Shared._Corvax.CCCVars;
using Content.Shared._Corvax.TTS;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private TTSTab? _ttsTab;

    private void RefreshVoiceTab()
    {
        if (!_cfgManager.GetCVar(CCCVars.TTSEnabled))
            return;

        _ttsTab = new TTSTab();
        var children = new List<Control>();
        foreach (var child in TabContainer.Children)
        {
            children.Add(child);
        }

        TabContainer.RemoveAllChildren();

        for (var i = 0; i < children.Count; i++)
        {
            if (i == 1) // Set the tab to the 2nd place.
            {
                TabContainer.AddChild(_ttsTab);
            }
            TabContainer.AddChild(children[i]);
        }

        TabContainer.SetTabTitle(1, Loc.GetString("humanoid-profile-editor-voice-tab"));

        _ttsTab.OnVoiceSelected += voiceId =>
        {
            SetVoice(voiceId);
            _ttsTab.SetSelectedVoice(voiceId);
        };

        _ttsTab.OnPreviewRequested += voiceId =>
        {
            _entManager.System<TTSSystem>().RequestPreviewTTS(voiceId);
        };
    }

    private void UpdateTTSVoicesControls()
    {
        if (Profile is null || _ttsTab is null)
            return;

        _ttsTab.UpdateControls(Profile, Profile.Sex, Profile.Species);
        _ttsTab.SetSelectedVoice(Profile.TTSVoice);
    }

    private void SetVoice(ProtoId<TTSVoicePrototype> newVoice)
    {
        Profile = Profile?.WithTTSVoice(newVoice);
        IsDirty = true;
    }
}
