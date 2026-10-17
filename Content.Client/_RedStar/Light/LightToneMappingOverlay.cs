using System.Numerics;
using Content.Client.Graphics;
using Content.Client.Overlays;
using Content.Shared.Light;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Light;

/// <summary>
/// Maps HDR lighting before it is multiplied into world textures and clipped by the scene buffer.
/// </summary>
public sealed partial class LightToneMappingOverlay : Overlay
{
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private ILightManager _lighting = default!;
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private static readonly ProtoId<ShaderPrototype> ShaderId = "LightToneMapping";
    private readonly OverlayResourceCache<CachedResources> _resources = new();
    private readonly ShaderInstance _shader;
    private readonly ShaderInstance _copyShader;

    // Clyde has completed the light buffer at this point, but has not drawn any world tiles/sprites.
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    public LightToneMappingOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = int.MinValue;
        _shader = _prototypes.Index(ShaderId).InstanceUnique();
        _copyShader = _prototypes.Index(ShaderId).InstanceUnique();
        _copyShader.SetParameter("ShadowStrength", 0f);
        _copyShader.SetParameter("CompressHighlights", false);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return args.Viewport.Eye is { DrawLight: true } && _lighting.Enabled && _lighting.DrawLighting &&
               !_overlays.HasOverlay<NightVisionOverlay>() &&
               _entities.TryGetComponent(args.MapUid, out MapComponent? map) && map.LightingEnabled &&
               (_config.GetCVar(LightingCVars.HighlightCompressionEnabled) ||
                _config.GetCVar(LightingCVars.DeepShadowsEnabled) && _config.GetCVar(LightingCVars.ShadowStrength) > 0);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.Viewport;
        var light = viewport.LightRenderTarget;
        var res = _resources.GetForViewport(viewport, static _ => new CachedResources());
        if (res.Buffer?.Size != light.Size)
        {
            res.Buffer?.Dispose();
            res.Buffer = _clyde.CreateLightRenderTarget(light.Size, "light-tone-mapping", depthStencil: false);
        }

        var strength = _config.GetCVar(LightingCVars.DeepShadowsEnabled)
            ? Math.Clamp(_config.GetCVar(LightingCVars.ShadowStrength), 0, 100) / 100f
            : 0f;
        _shader.SetParameter("ShadowStrength", strength);
        _shader.SetParameter("CompressHighlights", _config.GetCVar(LightingCVars.HighlightCompressionEnabled));

        var handle = args.WorldHandle;
        var scale = viewport.RenderScale * (light.Size / (Vector2) viewport.Size);
        var matrix = light.GetWorldToLocalMatrix(viewport.Eye!, scale);
        var bounds = args.WorldBounds;

        // Never sample the texture currently being written. Keep the intermediate buffer HDR
        // so disabling highlight compression does not silently clamp the original light values.
        handle.RenderInRenderTarget(res.Buffer, () =>
        {
            handle.SetTransform(matrix);
            handle.UseShader(_shader);
            handle.DrawTextureRect(light.Texture, bounds);
        }, Color.Transparent);

        handle.RenderInRenderTarget(light, () =>
        {
            handle.SetTransform(matrix);
            handle.UseShader(_copyShader);
            handle.DrawTextureRect(res.Buffer.Texture, bounds);
        }, Color.Transparent);

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _resources.Dispose();
        _shader.Dispose();
        _copyShader.Dispose();
        base.DisposeBehavior();
    }

    private sealed class CachedResources : IDisposable
    {
        public IRenderTexture? Buffer;
        public void Dispose() => Buffer?.Dispose();
    }
}
