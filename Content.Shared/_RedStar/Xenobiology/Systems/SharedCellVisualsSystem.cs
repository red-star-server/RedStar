using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.Visuals;

namespace Content.Shared._RedStar.Xenobiology.Systems;

public abstract partial class SharedCellVisualsSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnCellChanged(Entity<CellContainerVisualsComponent> ent, ref CellContainerChangedEvent args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<CellContainerVisualsComponent> ent)
    {
        if (!TryComp<CellContainerComponent>(ent, out var containerComponent))
            return;

        _appearance.SetData(ent, CellContainerVisuals.DishVisibility, !containerComponent.Empty);
        _appearance.SetData(ent, CellContainerVisuals.DishColor, GetAverageColor(containerComponent.Cells));
    }

    /// <summary>
    /// All cells contribute equally; an empty dish uses the sprite's default tint.
    /// </summary>
    public static Color GetAverageColor(IReadOnlyList<Cell> cells)
    {
        if (cells.Count == 0)
        {
            return Color.White;
        }

        var red = 0f;
        var green = 0f;
        var blue = 0f;
        var alpha = 0f;
        foreach (var cell in cells)
        {
            red += cell.Color.R;
            green += cell.Color.G;
            blue += cell.Color.B;
            alpha += cell.Color.A;
        }

        return new Color(red / cells.Count, green / cells.Count, blue / cells.Count, alpha / cells.Count);
    }
}
