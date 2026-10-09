using Content.Shared.Light;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Light;

public sealed partial class LightAtmosphereSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private LightAtmosphereOverlay? _overlay;
    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_config, LightAtmosphereCVars.Enabled, SetEnabled, true);
    }

    private void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            if (_overlay == null)
                return;

            _overlays.RemoveOverlay(_overlay);
            _overlay.Dispose();
            _overlay = null;
            return;
        }

        if (_overlay != null)
            return;

        _overlay = new LightAtmosphereOverlay(_prototypes);
        _overlays.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        SetEnabled(false);
        base.Shutdown();
    }
}
