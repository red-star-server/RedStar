using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Light;

/// <summary>
/// Accentuates strong local lighting and shapes shadows from the engine's light map.
/// </summary>
public sealed class LightAtmosphereOverlay(IPrototypeManager prototypes) : Overlay
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
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("LIGHT_TEXTURE", args.Viewport.LightRenderTarget.Texture);
        _shader.SetParameter("zoom", args.Viewport.Eye!.Zoom.X);
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
