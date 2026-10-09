using Content.Shared.Light;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Light;

public sealed partial class AtmosphericLightingSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private BloomLightTreeSystem _tree = default!;
    [Dependency] private SpriteSystem _sprites = default!;
    [Dependency] private TransformSystem _transforms = default!;

    private LightAtmosphereOverlay? _atmosphere;
    private BloomLightOverlay? _bloom;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_config, LightAtmosphereCVars.Enabled, SetEnabled, true);
    }

    private void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            if (_bloom != null)
            {
                _overlays.RemoveOverlay(_bloom);
                _bloom.Dispose();
                _bloom = null;
            }

            if (_atmosphere == null)
                return;

            _overlays.RemoveOverlay(_atmosphere);
            _atmosphere.Dispose();
            _atmosphere = null;

            return;
        }

        if (_atmosphere != null)
            return;

        _atmosphere = new LightAtmosphereOverlay(_prototypes);
        _bloom = new BloomLightOverlay(
            _tree,
            GetEntityQuery<PointLightComponent>(),
            _sprites,
            _transforms,
            _prototypes);
        _overlays.AddOverlay(_atmosphere);
        _overlays.AddOverlay(_bloom);
    }

    public override void Shutdown()
    {
        SetEnabled(false);
        base.Shutdown();
    }
}
