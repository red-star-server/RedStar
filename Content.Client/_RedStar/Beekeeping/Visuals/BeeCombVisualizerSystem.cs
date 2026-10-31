using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Robust.Client.GameObjects;

namespace Content.Client._RedStar.Beekeeping.Visuals;

/// <summary>
/// Tints the grey overlays of a honeycomb with the color of what is in it.
/// </summary>
public sealed class BeeCombVisualizerSystem : VisualizerSystem<BeeCombVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, BeeCombVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null ||
            !AppearanceSystem.TryGetData<Color>(uid, BeeCombVisuals.Color, out var color, args.Component))
        {
            return;
        }

        AppearanceSystem.TryGetData<float>(uid, BeeCombVisuals.Share, out var share, args.Component);
        var sprite = (uid, args.Sprite);
        Tint(sprite, BeeCombVisualLayers.Drips, component.DripsColor, color, share);
        Tint(sprite, BeeCombVisualLayers.Cells, component.CellsColor, color, share);
        Tint(sprite, BeeCombVisualLayers.Outline, component.OutlineColor, color, share);
    }

    /// <summary>
    /// The layer's own honey color moved towards the color of the other reagents by their share.
    /// </summary>
    private void Tint(Entity<SpriteComponent> sprite, BeeCombVisualLayers layer, Color honey, Color other, float share)
    {
        if (!SpriteSystem.LayerMapTryGet(sprite.AsNullable(), layer, out var index, false))
            return;

        var mixed = new Color(
            honey.R + (other.R - honey.R) * share,
            honey.G + (other.G - honey.G) * share,
            honey.B + (other.B - honey.B) * share);
        SpriteSystem.LayerSetColor(sprite.AsNullable(), index, mixed);
    }
}
