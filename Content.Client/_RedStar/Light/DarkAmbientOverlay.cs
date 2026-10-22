using Content.Client.Overlays;
using Content.Shared.Light;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Light;

/// <summary>
/// Grades the rendered world without changing the light map or interface.
/// </summary>
public sealed partial class DarkAmbientOverlay : Overlay
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IEyeManager _eyes = default!;
    [Dependency] private ILightManager _lighting = default!;
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "DarkAmbient";
    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public DarkAmbientOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypes.Index(Shader).InstanceUnique();
        // Grade the world before other world-space vision and status effects.
        ZIndex = -1;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _config.GetCVar(DarkAmbientCVars.Strength) > 0 &&
               args.Viewport.Eye == _eyes.CurrentEye &&
               args.Viewport.Eye is { DrawLight: true } &&
               _lighting.Enabled && _lighting.DrawLighting &&
               !_overlays.HasOverlay<NightVisionOverlay>() &&
               _entities.TryGetComponent(args.MapUid, out MapComponent? map) && map.LightingEnabled;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("Intensity", Math.Clamp(_config.GetCVar(DarkAmbientCVars.Strength), 0, 100) / 100f);

        var handle = args.WorldHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }
}
