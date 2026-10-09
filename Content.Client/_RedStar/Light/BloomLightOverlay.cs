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
/// Draws fixture halos over the lit world. The engine light map shades the masks with the world.
/// </summary>
public sealed class BloomLightOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShaderId = "LightBloom";

    private readonly BloomLightTreeSystem _tree;
    private readonly EntityQuery<PointLightComponent> _lights;
    private readonly SpriteSystem _sprites;
    private readonly TransformSystem _transforms;
    private readonly ShaderInstance _shader;
    private readonly Dictionary<SpriteSpecifier, Texture> _textures = [];
    private readonly List<LightToDraw> _visible = [];

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

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
        _tree.QueryAabb(ref state, Collect, args.MapId, args.WorldAABB.Enlarged(4f));
        return _visible.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        handle.UseShader(_shader);

        foreach (var light in _visible)
        {
            handle.SetTransform(light.Matrix);
            DrawMask(handle, light.Mask, light.Offset, light.Color, light.Emission);
            if (light.HaloMask != null)
                DrawMask(handle, light.HaloMask, light.HaloOffset, light.Color, light.Emission * 0.45f);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private static void DrawMask(DrawingHandleWorld handle, Texture texture, Vector2 offset, Color color, float maskStrength)
    {
        var size = new Vector2(texture.Width, texture.Height) / EyeManager.PixelsPerMeter;
        handle.DrawTexture(texture, offset - size / 2f, color.WithAlpha(color.A * maskStrength));
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
        // Scale the halo by emitted light, using the standard wall light as the reference.
        var emission = Math.Clamp(point.Energy * point.Radius * point.Radius / 60f, 0f, 1f);
        overlay._visible.Add(new LightToDraw(
            matrix,
            mask,
            bloom.MaskOffset,
            bloom.HaloMask is { } halo ? overlay.GetTexture(halo) : null,
            bloom.HaloOffset,
            point.Color,
            emission));
        return true;
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }

    private readonly record struct QueryState(BloomLightOverlay Overlay);

    private readonly record struct LightToDraw(
        Matrix3x2 Matrix,
        Texture Mask,
        Vector2 Offset,
        Texture? HaloMask,
        Vector2 HaloOffset,
        Color Color,
        float Emission);
}
