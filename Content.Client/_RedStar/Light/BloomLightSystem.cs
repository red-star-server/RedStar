using Content.Shared.Light;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Light;

public sealed partial class BloomLightSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private BloomLightTreeSystem _tree = default!;
    [Dependency] private SpriteSystem _sprites = default!;
    [Dependency] private TransformSystem _transforms = default!;

    private BloomLightOverlay? _bloom;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_config, LightingCVars.BloomEnabled, SetBloomEnabled, true);
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

    public override void Shutdown()
    {
        SetBloomEnabled(false);
        base.Shutdown();
    }
}
