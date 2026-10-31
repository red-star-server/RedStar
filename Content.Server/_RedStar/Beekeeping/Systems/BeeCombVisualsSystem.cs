using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// Keeps a honeycomb's tint in step with the reagents in it: how much of it is not honey, and their mixed color.
/// </summary>
public sealed partial class BeeCombVisualsSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BeeCombVisualsComponent> ent, ref MapInitEvent args)
    {
        if (_solutions.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution))
            Update(ent, solution);
    }

    [SubscribeLocalEvent]
    private void OnSolutionChanged(Entity<BeeCombVisualsComponent> ent, ref SolutionChangedEvent args)
    {
        if (args.Solution.Comp.Id != ent.Comp.Solution)
            return;

        Update(ent, args.Solution.Comp.Solution);
    }

    private void Update(Entity<BeeCombVisualsComponent> ent, Solution solution)
    {
        float r = 0f, g = 0f, b = 0f, other = 0f, total = 0f;
        foreach (var (reagent, quantity) in solution.Contents)
        {
            var amount = quantity.Float();
            total += amount;
            if (ent.Comp.HoneyReagents.Contains(reagent.Prototype) ||
                !_proto.TryIndex(reagent.Prototype, out var proto))
            {
                continue;
            }

            r += proto.SubstanceColor.R * amount;
            g += proto.SubstanceColor.G * amount;
            b += proto.SubstanceColor.B * amount;
            other += amount;
        }

        _appearance.SetData(ent, BeeCombVisuals.Color, other > 0f ? new Color(r / other, g / other, b / other) : Color.White);
        _appearance.SetData(ent, BeeCombVisuals.Share, total > 0f ? other / total : 0f);
    }
}
