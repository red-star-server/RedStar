using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Robust.Client.GameObjects;

namespace Content.Client._RedStar.Beekeeping.Visuals;

/// <summary>
/// Shows a colony's traits on a queen or bee: the grey mask takes the payload colour (honey without one) and the
/// angry eyes show for aggressive bees. Sprites without those layers are left alone.
/// </summary>
public sealed class BeeTraitVisualizerSystem : VisualizerSystem<BeeTraitVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid,
        BeeTraitVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var sprite = (uid, args.Sprite);

        if (AppearanceSystem.TryGetData<Color>(uid, BeeTraitVisuals.GreyColor, out var color, args.Component) &&
            SpriteSystem.LayerMapTryGet(sprite, BeeForagerVisualLayers.Grey, out var grey, false))
        {
            SpriteSystem.LayerSetColor(sprite, grey, color);
        }

        if (AppearanceSystem.TryGetData<bool>(uid, BeeTraitVisuals.Angry, out var angry, args.Component) &&
            SpriteSystem.LayerMapTryGet(sprite, BeeForagerVisualLayers.Angry, out var eyes, false))
        {
            SpriteSystem.LayerSetVisible(sprite, eyes, angry);
        }
    }
}
