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

    private LightHighlightOverlay? _highlights;
    private BloomLightOverlay? _bloom;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_config, LightAtmosphereCVars.HighlightsEnabled, SetHighlightsEnabled, true);
        Subs.CVar(_config, LightAtmosphereCVars.BloomEnabled, SetBloomEnabled, true);
    }

    private void SetBloomEnabled(bool enabled)
    {
        if (!enabled)
        {
            if (_bloom != null)
            {
                _overlays.RemoveOverlay(_bloom);
                _bloom.Dispose();
                _bloom = null;
            }

            return;
        }

        if (_bloom != null)
            return;

        _bloom = new BloomLightOverlay(
            _tree,
            GetEntityQuery<PointLightComponent>(),
            _sprites,
            _transforms,
            _prototypes);
        _overlays.AddOverlay(_bloom);
    }

    private void SetHighlightsEnabled(bool enabled)
    {
        if (!enabled)
        {
            if (_highlights == null)
                return;

            _overlays.RemoveOverlay(_highlights);
            _highlights.Dispose();
            _highlights = null;
            return;
        }

        if (_highlights != null)
            return;

        _highlights = new LightHighlightOverlay(_prototypes, _overlays, EntityManager);
        _overlays.AddOverlay(_highlights);
    }

    public override void Shutdown()
    {
        SetHighlightsEnabled(false);
        SetBloomEnabled(false);
        base.Shutdown();
    }
}
