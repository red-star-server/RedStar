using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Visuals;
using Robust.Client.GameObjects;

namespace Content.Client._RedStar.Xenobiology.Systems;

public sealed partial class SampleVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChanged(Entity<SampleContainerVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var color = args.AppearanceData.TryGetValue(SampleContainerVisuals.DishColor, out var value) && value is Color tint
            ? tint
            : Color.White;
        _sprite.LayerSetColor((ent.Owner, args.Sprite), SampleContainerVisuals.DishLayer, color);
    }
}
