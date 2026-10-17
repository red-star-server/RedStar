using System.Numerics;
using Content.Client.Light.EntitySystems;
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
/// Grades lighting under roofs while leaving the outdoor scene unchanged.
/// </summary>
public sealed class LightAtmosphereOverlay(IPrototypeManager prototypes, IEntityManager entities) : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShaderId = "LightAtmosphere";

    private readonly ShaderInstance _shader = prototypes.Index(ShaderId).InstanceUnique();
    private readonly ShaderInstance _unshaded = prototypes.Index<ShaderPrototype>("unshaded").Instance();
    private readonly GridStencilSystem _stencils = entities.System<GridStencilSystem>();
    private readonly SharedRoofSystem _roofs = entities.System<SharedRoofSystem>();
    private readonly TurfSystem _turf = entities.System<TurfSystem>();

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
        handle.UseShader(_unshaded);
        var roofMask = _stencils.GetTileStencil(args, "light-atmosphere-roofs",
            "light-atmosphere-roofs", IsRoofed);
        handle.SetTransform(Matrix3x2.Identity);
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("LIGHT_TEXTURE", args.Viewport.LightRenderTarget.Texture);
        _shader.SetParameter("ROOF_TEXTURE", roofMask.Texture);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    private bool IsRoofed(Entity<MapGridComponent> grid, TileRef tile)
    {
        if (_turf.IsSpace(tile))
            return false;

        if (entities.HasComponent<ImplicitRoofComponent>(grid.Owner))
            return true;

        return entities.TryGetComponent(grid.Owner, out RoofComponent? roof) &&
               _roofs.IsRooved((grid.Owner, grid.Comp, roof), tile.GridIndices);
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }
}
