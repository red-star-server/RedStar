using Content.Shared.Light;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client.Light;

public sealed partial class DarkAmbientSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IOverlayManager _overlays = default!;

    private DarkAmbientOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_config, DarkAmbientCVars.Enabled, SetEnabled, true);
    }

    private void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            if (_overlay != null)
                return;

            _overlay = new DarkAmbientOverlay();
            _overlays.AddOverlay(_overlay);
        }
        else if (_overlay != null)
        {
            _overlays.RemoveOverlay(_overlay);
            _overlay.Dispose();
            _overlay = null;
        }
    }

    public override void Shutdown()
    {
        SetEnabled(false);
        base.Shutdown();
    }
}
