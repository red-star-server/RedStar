using Content.Shared.Light;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client.Light;

public sealed partial class BloomLightSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private BloomLightTreeSystem _tree = default!;
    [Dependency] private SpriteSystem _sprites = default!;
    [Dependency] private TransformSystem _transforms = default!;

    private BloomLightOverlay? _overlay;
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

        _overlay = new BloomLightOverlay(
            _tree,
            GetEntityQuery<PointLightComponent>(),
            _sprites,
            _transforms);
        _overlays.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        SetEnabled(false);
        base.Shutdown();
    }
}
