using System.Numerics;
using Content.Client.Light.EntitySystems;
using Content.Client.Overlays;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Maps;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Light;

/// <summary>
/// Lifts local light above the ambient background independently of world color grading.
/// </summary>
public sealed class LightHighlightOverlay(IPrototypeManager prototypes, IOverlayManager overlays, IEntityManager entities) : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShaderId = "LightHighlights";
    private static readonly ProtoId<ShaderPrototype> UnshadedId = "unshaded";
    private readonly ShaderInstance _shader = prototypes.Index(ShaderId).InstanceUnique();
    private readonly ShaderInstance _unshaded = prototypes.Index(UnshadedId).Instance();
    private readonly GridStencilSystem _stencils = entities.System<GridStencilSystem>();
    private readonly SharedRoofSystem _roofs = entities.System<SharedRoofSystem>();
    private readonly TurfSystem _turf = entities.System<TurfSystem>();
    private readonly ILightManager _lighting = IoCManager.Resolve<ILightManager>();

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    public override bool RequestScreenTexture => true;

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return args.Viewport.Eye is { DrawLight: true } && _lighting.Enabled && _lighting.DrawLighting &&
               !overlays.HasOverlay<NightVisionOverlay>() &&
               entities.TryGetComponent(args.MapUid, out MapComponent? map) && map.LightingEnabled;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        // The enlarged target contains background/roof/tile light before point lights.
        // Use exactly the crop copied into LightRenderTarget by AfterLightTargetOverlay.
        var ambient = overlays.GetOverlay<BeforeLightTargetOverlay>()
            .GetCachedForViewport(args.Viewport).EnlargedLightTarget;
        var lightSize = args.Viewport.LightRenderTarget.Size;
        var border = (ambient.Size - lightSize) / 2;
        var handle = args.WorldHandle;
        handle.UseShader(_unshaded);
        var roof = _stencils.GetTileStencil(args, "light-highlights-roofs", "light-highlights-roofs", IsRoofed);
        handle.SetTransform(Matrix3x2.Identity);
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("LIGHT_TEXTURE", args.Viewport.LightRenderTarget.Texture);
        _shader.SetParameter("AMBIENT_TEXTURE", ambient.Texture);
        _shader.SetParameter("ROOF_TEXTURE", roof.Texture);
        _shader.SetParameter("ambient_uv_scale", lightSize / (Vector2) ambient.Size);
        _shader.SetParameter("ambient_uv_offset", border / (Vector2) ambient.Size);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    private bool IsRoofed(Entity<MapGridComponent> grid, TileRef tile)
    {
        return !_turf.IsSpace(tile) &&
               (entities.HasComponent<ImplicitRoofComponent>(grid.Owner) ||
                entities.TryGetComponent(grid.Owner, out RoofComponent? roof) &&
                _roofs.IsRooved((grid.Owner, grid.Comp, roof), tile.GridIndices));
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }
}
