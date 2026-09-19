using Content.Client._Corvax.Options.UI.Tabs;
using Content.Shared._Corvax.CCCVars;

namespace Content.Client.Options.UI.Tabs;

public sealed partial class AudioTab
{
    private TTSControl _ttsControl = default!;

    private void BuildTtsBlock()
    {
        if (!_cfg.GetCVar(CCCVars.TTSEnabled))
            return;

        _ttsControl = new TTSControl();
        _ttsControl.Initialize(Control);

        AudioBox.AddChild(_ttsControl);
    }
}
