using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Light;

/// <summary>
/// Accentuates local lighting above the existing ambient light without grading the scene again.
/// </summary>
public sealed class LightAtmosphereOverlay(IPrototypeManager prototypes, IOverlayManager overlays) : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShaderId = "LightAtmosphere";

    private readonly ShaderInstance _shader = prototypes.Index(ShaderId).InstanceUnique();

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;
    public override bool RequestScreenTexture => true;

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return args.Viewport.Eye is { DrawLight: true };
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        var ambient = overlays.GetOverlay<BeforeLightTargetOverlay>()
            .GetCachedForViewport(args.Viewport).EnlargedLightTarget;
        var lightSize = args.Viewport.LightRenderTarget.Size;
        var halfBorder = (ambient.Size - lightSize) / 2;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("LIGHT_TEXTURE", args.Viewport.LightRenderTarget.Texture);
        _shader.SetParameter("AMBIENT_TEXTURE", ambient.Texture);
        _shader.SetParameter("ambient_uv_scale", lightSize / (Vector2) ambient.Size);
        _shader.SetParameter("ambient_uv_offset", halfBorder / (Vector2) ambient.Size);
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
