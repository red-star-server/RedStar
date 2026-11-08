using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Robust.Client.GameObjects;

namespace Content.Client._RedStar.Beekeeping.Visuals;

/// <summary>
/// Tints the comb and outline overlays of a hive frame with the color of the reagents in its combs.
/// </summary>
public sealed class BeeFrameVisualizerSystem : VisualizerSystem<BeeFrameComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, BeeFrameComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null ||
            !AppearanceSystem.TryGetData<Color>(uid, BeeFrameVisuals.Color, out var color, args.Component))
        {
            return;
        }

        var sprite = (uid, args.Sprite);
        foreach (var layer in new[] { BeeFrameVisualLayers.Comb, BeeFrameVisualLayers.Outline })
        {
            if (SpriteSystem.LayerMapTryGet(sprite, layer, out var index, false))
                SpriteSystem.LayerSetColor(sprite, index, color);
        }
    }
}
