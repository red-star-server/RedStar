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
        var other = 0f;
        foreach (var (reagent, quantity) in solution.Contents)
        {
            var amount = quantity.Float();
            if (ent.Comp.HoneyReagents.Contains(reagent.Prototype) ||
                !_proto.HasIndex(reagent.Prototype))
            {
                continue;
            }

            other += amount;
        }

        var otherColor = other > 0f
            ? solution.GetColorWithout(_proto, ent.Comp.HoneyReagents.ToArray())
            : Color.White;
        _appearance.SetData(ent, BeeCombVisuals.Color, otherColor);
        _appearance.SetData(ent, BeeCombVisuals.Share, solution.Volume > 0 ? other / solution.Volume.Float() : 0f);
    }
}
