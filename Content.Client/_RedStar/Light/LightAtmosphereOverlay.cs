using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Light;

/// <summary>
/// Lifts illuminated surfaces using the color of the local light buffer.
/// </summary>
public sealed class LightAtmosphereOverlay(IPrototypeManager prototypes) : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShaderId = "LightAtmosphere";

    private readonly ShaderInstance _shader = prototypes.Index(ShaderId).InstanceUnique();

    public float Strength = 0.55f;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return Strength > 0f && args.Viewport.Eye is { DrawLight: true };
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("LIGHT_TEXTURE", args.Viewport.LightRenderTarget.Texture);
        _shader.SetParameter("strength", Strength);
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
