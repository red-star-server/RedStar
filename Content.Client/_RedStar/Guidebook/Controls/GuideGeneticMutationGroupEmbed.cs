using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Guidebook.Richtext;
using Content.Shared._RedStar.Genetics.Prototypes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._RedStar.Guidebook.Controls;

[UsedImplicitly]
public sealed partial class GuideGeneticMutationGroupEmbed : BoxContainer, IDocumentTag
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    public GuideGeneticMutationGroupEmbed()
    {
        IoCManager.InjectDependencies(this);
        Orientation = LayoutOrientation.Vertical;
        MouseFilter = MouseFilterMode.Stop;
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        int? tier = null;
        if (args.TryGetValue("Tier", out var value))
        {
            if (!int.TryParse(value, out var parsed) || parsed < 1)
            {
                Logger.GetSawmill("guidebook.genetics").Error("GuideGeneticMutationGroupEmbed requires a positive integer Tier.");
                return false;
            }
            tier = parsed;
        }

        RemoveAllChildren();
        var recipes = _prototypes.EnumeratePrototypes<GeneticMutationPrototype>()
            .Where(recipe => tier is null || _prototypes.Index(recipe.ResultGene).Tier == tier)
            .OrderBy(recipe => _prototypes.Index(recipe.ResultGene).Tier)
            .ThenBy(recipe => Loc.GetString($"gene-{recipe.ResultGene}-name"))
            .ThenBy(recipe => recipe.ID, StringComparer.Ordinal);
        foreach (var recipe in recipes)
        {
            AddChild(new GuideGeneticMutationEmbed(recipe));
        }

        control = this;
        return true;
    }
}
