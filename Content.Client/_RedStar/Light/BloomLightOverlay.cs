using System.Numerics;
using Content.Shared.Light;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Light;

/// <summary>
/// Draws one fixture mask over the lit world. The engine light map shades it with the world.
/// </summary>
public sealed class BloomLightOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShaderId = "BloomLightMask";

    private readonly BloomLightTreeSystem _tree;
    private readonly EntityQuery<PointLightComponent> _lights;
    private readonly SpriteSystem _sprites;
    private readonly TransformSystem _transforms;
    private readonly ShaderInstance _shader;
    private readonly Dictionary<SpriteSpecifier, Texture> _textures = [];
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
        _tree = tree;
        _lights = lights;
        _sprites = sprites;
        _transforms = transforms;
        _shader = prototypes.Index(ShaderId).InstanceUnique();
        ZIndex = (int) DrawDepth.Effects;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _visible.Clear();
        var state = new QueryState(this);
        _tree.QueryAabb(ref state, Collect, args.MapId, args.WorldAABB);
        return _visible.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        handle.UseShader(_shader);
        foreach (var light in _visible)
        {
            handle.SetTransform(light.Matrix);
            DrawMask(handle, light.Mask, light.Offset, light.Color.WithAlpha(light.Color.A * light.Opacity));
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }

    private static void DrawMask(DrawingHandleWorld handle, Texture texture, Vector2 offset, Color color)
    {
        var size = new Vector2(texture.Width, texture.Height) / EyeManager.PixelsPerMeter;
        handle.DrawTexture(texture, offset - size / 2f, color);
    }

    private Texture GetTexture(SpriteSpecifier sprite)
    {
        if (_textures.TryGetValue(sprite, out var texture))
            return texture;

        texture = _sprites.Frame0(sprite);
        _textures.Add(sprite, texture);
        return texture;
    }

    private static bool Collect(ref QueryState state, in ComponentTreeEntry<BloomLightComponent> entry)
    {
        var overlay = state.Overlay;
        if (!overlay._lights.TryComp(entry.Uid, out var point) || !point.Enabled)
            return true;

        var bloom = entry.Component;
        var (_, _, matrix) = overlay._transforms.GetWorldPositionRotationMatrix(entry.Transform);
        var mask = overlay.GetTexture(bloom.Mask);
        overlay._visible.Add(new LightToDraw(
            matrix,
            mask,
            bloom.MaskOffset,
            point.Color,
            bloom.MaskOpacity));
        return true;
    }

    private readonly record struct QueryState(BloomLightOverlay Overlay);

    private readonly record struct LightToDraw(
        Matrix3x2 Matrix,
        Texture Mask,
        Vector2 Offset,
        Color Color,
        float Opacity);
}
