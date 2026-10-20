using System.Linq;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Data;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.Visuals;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class SampleVisualsSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnSampleChanged(Entity<SampleContainerVisualsComponent> ent, ref SampleContainerChangedEvent args)
    {
        UpdateAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SampleContainerVisualsComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<SampleContainerVisualsComponent> ent)
    {
        if (!TryComp<SampleContainerComponent>(ent, out var containerComponent))
            return;

        _appearance.SetData(ent, SampleContainerVisuals.DishVisibility,
            containerComponent.Samples.Any(sample => sample.GeneCount > 0));
        _appearance.SetData(ent, SampleContainerVisuals.DishColor, GetAverageSampleColor(containerComponent.Samples));
    }

    /// <summary>
    /// All samples contribute equally; an empty dish uses the sprite's default tint.
    /// </summary>
    private static Color GetAverageSampleColor(IReadOnlyList<BiologicalSample> samples)
    {
        if (samples.Count == 0)
        {
            return Color.White;
        }

        var red = 0f;
        var green = 0f;
        var blue = 0f;
        var alpha = 0f;
        foreach (var sample in samples)
        {
            red += sample.Color.R;
            green += sample.Color.G;
            blue += sample.Color.B;
            alpha += sample.Color.A;
        }

        return new Color(red / samples.Count, green / samples.Count, blue / samples.Count, alpha / samples.Count);
    }
}
