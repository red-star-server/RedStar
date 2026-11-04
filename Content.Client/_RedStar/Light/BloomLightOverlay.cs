using System.Numerics;
using Content.Client.Graphics;
using Content.Client.Overlays;
using Content.Shared.Light;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Light;

/// <summary>
/// Combines local fixture glow before compositing it over the world.
/// </summary>
public sealed partial class BloomLightOverlay : Overlay
{
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private ILightManager _lighting = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IConfigurationManager _config = default!;
    private static readonly ProtoId<ShaderPrototype> ShaderId = "BloomLightMask";
    private static readonly ProtoId<ShaderPrototype> CompositeShaderId = "BloomLightComposite";

    private readonly BloomLightTreeSystem _tree;
    private readonly EntityQuery<PointLightComponent> _lights;
    private readonly SpriteSystem _sprites;
    private readonly TransformSystem _transforms;
    private readonly IPrototypeManager _prototypes;
    private readonly ShaderInstance _composite;
    private readonly OverlayResourceCache<CachedResources> _resources = new();
    private readonly Dictionary<SpriteSpecifier, CachedMask> _textures = [];
    private readonly List<LightToDraw> _visible = [];

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    public override bool RequestScreenTexture => true;

    public BloomLightOverlay(
        BloomLightTreeSystem tree,
        EntityQuery<PointLightComponent> lights,
        SpriteSystem sprites,
        TransformSystem transforms,
        IPrototypeManager prototypes)
    {
        IoCManager.InjectDependencies(this);
        _tree = tree;
        _lights = lights;
        _sprites = sprites;
        _transforms = transforms;
        _prototypes = prototypes;
        _composite = prototypes.Index(CompositeShaderId).InstanceUnique();
        ZIndex = (int) DrawDepth.Effects;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _visible.Clear();
        if (_config.GetCVar(LightingCVars.BloomStrength) <= 0 ||
            args.Viewport.Eye is not { DrawLight: true } || !_lighting.Enabled || !_lighting.DrawLighting ||
            _overlays.HasOverlay<NightVisionOverlay>() ||
            !_entities.TryGetComponent(args.MapUid, out MapComponent? map) || !map.LightingEnabled)
            return false;

        var strength = Math.Clamp(_config.GetCVar(LightingCVars.BloomStrength), 0, 100) / 100f;
        var state = new QueryState(this, strength);
        _tree.QueryAabb(ref state, Collect, args.MapId, args.WorldAABB);
        return _visible.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var viewport = args.Viewport;
        var res = _resources.GetForViewport(viewport, static _ => new CachedResources());
        if (res.Transmittance?.Size != viewport.RenderTarget.Size)
        {
            res.Transmittance?.Dispose();
            res.Transmittance = _clyde.CreateRenderTarget(viewport.RenderTarget.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba16F),
                new TextureSampleParameters { Filter = true }, name: "fixture-glow");
        }

        var target = res.Transmittance;
        var scale = viewport.RenderScale * (target.Size / (Vector2) viewport.Size);
        var worldToTarget = target.GetWorldToLocalMatrix(viewport.Eye!, scale);
        var handle = args.WorldHandle;
        foreach (var mask in _textures.Values)
        {
            mask.Shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        }

        handle.RenderInRenderTarget(target, () =>
        {
            foreach (var light in _visible)
            {
                handle.SetTransform(light.Matrix * worldToTarget);
                handle.UseShader(light.Mask.Shader);
                handle.DrawTextureRect(light.Mask.Texture, light.Bounds, light.Color);
            }
        }, Color.White);

        handle.SetTransform(Matrix3x2.Identity);
        _composite.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        handle.UseShader(_composite);
        handle.DrawTextureRect(target.Texture, args.WorldBounds);
        handle.UseShader(null);
    }

    protected override void DisposeBehavior()
    {
        _resources.Dispose();
        _composite.Dispose();
        foreach (var mask in _textures.Values)
        {
            mask.Shader.Dispose();
        }

        base.DisposeBehavior();
    }

    private CachedMask GetMask(SpriteSpecifier sprite)
    {
        if (_textures.TryGetValue(sprite, out var texture))
            return texture;

        var frame = _sprites.Frame0(sprite);
        var shader = _prototypes.Index(ShaderId).InstanceUnique();
        shader.SetParameter("MaskSize", new Vector2(frame.Width, frame.Height));
        texture = new CachedMask(frame, shader);
        _textures.Add(sprite, texture);
        return texture;
    }

    private static bool Collect(ref QueryState state, in ComponentTreeEntry<BloomLightComponent> entry)
    {
        var overlay = state.Overlay;
        if (!overlay._lights.TryComp(entry.Uid, out var point) || !point.Enabled ||
            point.ContainerOccluded || point.Energy <= 0f || point.Radius <= 0f)
            return true;

        var bloom = entry.Component;
        if (!bloom.Enabled || bloom.MaskOpacity <= 0f)
            return true;

        var (_, _, matrix) = overlay._transforms.GetWorldPositionRotationMatrix(entry.Transform);
        // Follow flicker/fades without allowing high-energy lamps to produce unbounded haze.
        var energy = 2f * point.Energy / (1f + point.Energy);
        var color = point.Color * bloom.Tint;
        var mask = overlay.GetMask(bloom.Mask);
        var size = new Vector2(mask.Texture.Width, mask.Texture.Height) / EyeManager.PixelsPerMeter;
        var bounds = Box2.FromDimensions(bloom.MaskOffset - size / 2f, size);
        var opacity = Math.Clamp(color.A * bloom.MaskOpacity * energy * state.Strength, 0f, 1f);
        overlay._visible.Add(new LightToDraw(matrix, mask, bounds, color.WithAlpha(opacity)));

        return true;
    }

    private readonly record struct QueryState(BloomLightOverlay Overlay, float Strength);

    private readonly record struct LightToDraw(
        Matrix3x2 Matrix,
        CachedMask Mask,
        Box2 Bounds,
        Color Color);

    private readonly record struct CachedMask(Texture Texture, ShaderInstance Shader);

    private sealed class CachedResources : IDisposable
    {
        public IRenderTexture? Transmittance;
        public void Dispose() => Transmittance?.Dispose();
    }
}
