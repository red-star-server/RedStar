using System.Linq;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.Visuals;

namespace Content.Server._RedStar.Xenobiology;

public sealed partial class SampleVisualsSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnSampleChanged(Entity<CellContainerVisualsComponent> ent, ref SampleContainerChangedEvent args)
    {
        UpdateAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CellContainerVisualsComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<CellContainerVisualsComponent> ent)
    {
        if (!TryComp<SampleContainerComponent>(ent, out var containerComponent))
            return;

        _appearance.SetData(ent, CellContainerVisuals.DishVisibility,
            containerComponent.Samples.Any(sample => sample.GeneCount > 0));
        _appearance.SetData(ent, CellContainerVisuals.DishColor, GetAverageSampleColor(containerComponent.Samples));
    }

    /// <summary>
    /// All cells contribute equally; an empty dish uses the sprite's default tint.
    /// </summary>
    private static Color GetAverageSampleColor(IReadOnlyList<BiologicalSample> cells)
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
