using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Systems;
using Content.Shared._RedStar.Xenobiology.Visuals;
using Robust.Client.GameObjects;

namespace Content.Client._RedStar.Xenobiology;

public sealed partial class CellVisualsSystem : SharedCellVisualsSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChanged(Entity<CellContainerVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var color = args.AppearanceData.TryGetValue(CellContainerVisuals.DishColor, out var value) && value is Color tint
            ? tint
            : Color.White;
        _sprite.LayerSetColor((ent.Owner, args.Sprite), CellContainerVisuals.DishLayer, color);
    }
}
