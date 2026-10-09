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
/// Draws fixture halos over the lit world. The shader uses the screen image to soften halos in darkness.
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

    public float Strength = 0.7f;
    public bool Cones = true;

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
        if (Strength <= 0f)
            return false;

        var state = new QueryState(this);
        _tree.QueryAabb(ref state, Collect, args.MapId, args.WorldAABB.Enlarged(4f));
        return _visible.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("strength", Strength);
        handle.UseShader(_shader);

        foreach (var light in _visible)
        {
            handle.SetTransform(light.Matrix);
            if (Cones && light.Cone != null)
                DrawMask(handle, light.Cone, light.ConeOffset, light.Color, 0.55f);

            DrawMask(handle, light.Point, light.PointOffset, light.Color, 1f);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawMask(DrawingHandleWorld handle, Texture texture, Vector2 offset, Color color, float maskStrength)
    {
        _shader.SetParameter("mask_strength", maskStrength);
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
        var cone = bloom.ShowCone ? overlay.GetTexture(bloom.ConeMask) : null;
        overlay._visible.Add(new LightToDraw(
            matrix,
            overlay.GetTexture(bloom.PointMask),
            cone,
            bloom.PointOffset,
            bloom.ConeOffset,
            point.Color));
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
        Texture Point,
        Texture? Cone,
        Vector2 PointOffset,
        Vector2 ConeOffset,
        Color Color);
}
