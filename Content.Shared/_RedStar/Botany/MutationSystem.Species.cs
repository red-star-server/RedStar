using System.Linq;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// RedStar's directed species selection runs only when the existing ChangeSpecies effect fires.
/// </summary>
public sealed partial class PlantMutationSystem
{
    [PublicAPI]
    public void TrySpeciesChange(Entity<PlantDataComponent?> plant)
    {
        if (!_net.IsServer || !Resolve(plant, ref plant.Comp, false) || plant.Comp.Mutations.Count == 0 ||
            !_plantQuery.TryComp(plant.Owner, out var genetics))
            return;

        Entity<PlantTrayComponent>? tray = _plant.TryGetTray(plant.Owner, out var trayEnt) ? trayEnt : null;
        var environment = new PlantMutationEnvironmentEvent();
        RaiseLocalEvent(plant.Owner, ref environment);

        var passed = new List<PlantSpeciesMutation>();
        foreach (var edge in plant.Comp.Mutations)
        {
            if (RequirementsMet(plant.Owner, genetics, tray, environment, edge.Requirements) && Random(edge.Chance))
                passed.Add(edge);
        }

        if (passed.Count == 0)
            return;

        var roll = _random.NextFloat() * passed.Sum(edge => edge.Weight);
        var chosen = passed[^1];
        foreach (var edge in passed)
        {
            roll -= edge.Weight;
            if (roll > 0f)
                continue;

            chosen = edge;
            break;
        }

        if (SpeciesChange(plant, chosen.Target) && tray is { } selectedTray)
            ConsumeReagents(selectedTray, chosen.Requirements.Reagents);
    }

    private bool RequirementsMet(EntityUid plantUid, PlantComponent genetics, Entity<PlantTrayComponent>? tray,
        PlantMutationEnvironmentEvent environment, PlantSpeciesMutationRequirements requirements)
    {
        if (!Within(genetics.Potency, requirements.MinPotency, requirements.MaxPotency) ||
            !Within(genetics.GeneticInstability, requirements.MinGeneticInstability, requirements.MaxGeneticInstability))
            return false;

        if (requirements.MinWater != null || requirements.MaxWater != null ||
            requirements.MinNutrients != null || requirements.MaxNutrients != null)
        {
            if (tray is not { } plantedTray ||
                !Within(plantedTray.Comp.WaterLevel, requirements.MinWater, requirements.MaxWater) ||
                !Within(plantedTray.Comp.NutritionLevel, requirements.MinNutrients, requirements.MaxNutrients))
                return false;
        }

        foreach (var trait in requirements.RequiredTraits)
        {
            if (!Factory.TryGetRegistration(trait, out var registration) || !HasComp(plantUid, registration.Type))
                return false;
        }

        if (requirements.Reagents.Count > 0)
        {
            if (tray is not { } plantedTray ||
                !_solutions.TryGetSolution(plantedTray.Owner, plantedTray.Comp.SoilSolutionName, out _, out var solution))
                return false;

            foreach (var reagent in requirements.Reagents)
            {
                var amount = solution.Contents.Where(entry => entry.Reagent.Prototype == reagent.Id.Id)
                    .Sum(entry => entry.Quantity.Float());
                if (amount < reagent.MinAmount)
                    return false;
            }
        }

        if (requirements.Environment is not { } external)
            return true;

        if ((external.MinTemperature != null || external.MaxTemperature != null || external.Gases.Count > 0) &&
            environment.Atmosphere is null)
            return false;

        if ((external.MinTemperature != null || external.MaxTemperature != null) &&
            !Within(environment.Atmosphere!.Temperature, external.MinTemperature, external.MaxTemperature))
            return false;

        foreach (var gas in external.Gases)
        {
            if (!Within(environment.Atmosphere!.GetMoles(gas.Id), gas.MinAmount, gas.MaxAmount))
                return false;
        }

        if ((external.MinLight != null || external.MaxLight != null) &&
            (environment.Light is not { } light || !Within(light, external.MinLight, external.MaxLight)))
            return false;

        return true;
    }

    private void ConsumeReagents(Entity<PlantTrayComponent> tray, List<PlantMutationReagentRequirement> reagents)
    {
        if (reagents.All(reagent => reagent.ConsumeAmount <= 0f) ||
            !_solutions.TryGetSolution(tray.Owner, tray.Comp.SoilSolutionName, out var solutionEnt, out var solution))
            return;

        foreach (var reagent in reagents)
        {
            var remaining = FixedPoint2.New(reagent.ConsumeAmount);
            foreach (var entry in solution.Contents.ToArray())
            {
                if (remaining <= FixedPoint2.Zero)
                    break;

                if (entry.Reagent.Prototype != reagent.Id.Id)
                    continue;

                remaining -= _solutions.RemoveReagent(solutionEnt.Value, entry.Reagent,
                    FixedPoint2.Min(remaining, entry.Quantity));
            }
        }
    }

    private static bool Within(float value, float? min, float? max) =>
        (min is null || value >= min.Value) && (max is null || value <= max.Value);
}
