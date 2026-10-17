using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Guidebook.Controls;
using Content.Client.Guidebook.Richtext;
using Content.Client.UserInterface.ControlExtensions;
using Content.Shared._RedStar.Genetics;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._RedStar.Guidebook.Controls;

[UsedImplicitly]
public sealed partial class GuideGeneGroupEmbed : BoxContainer, IDocumentTag
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    public GuideGeneGroupEmbed()
    {
        IoCManager.InjectDependencies(this);
        Orientation = LayoutOrientation.Vertical;
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!args.TryGetValue("Tier", out var value) || !int.TryParse(value, out var tier) || tier < 1)
        {
            Logger.GetSawmill("guidebook.genetics").Error("GuideGeneGroupEmbed requires a positive integer Tier.");
            return false;
        }

        RemoveAllChildren();
        foreach (var gene in _prototypes.EnumeratePrototypes<GenePrototype>()
                     .Where(gene => gene.Tier == tier)
                     .OrderBy(gene => Loc.GetString($"gene-{gene.ID}-name")))
        {
            AddChild(new GeneEntry(gene));
        }

        control = this;
        return true;
    }

    private sealed class GeneEntry : BoxContainer, ISearchableControl, IPrototypeRepresentationControl
    {
        public IPrototype? RepresentedPrototype { get; }

        public GeneEntry(GenePrototype gene)
        {
            RepresentedPrototype = gene;
            Margin = new Thickness(10, 4);
            AddChild(new Label
            {
                Text = Loc.GetString($"gene-{gene.ID}-name"),
                HorizontalExpand = true,
            });
            AddChild(new Label
            {
                Text = Loc.GetString("guidebook-genetic-mutation-tier", ("tier", gene.Tier)),
            });
        }

        public bool CheckMatchesSearch(string query) => this.ChildrenContainText(query);

        public void SetHiddenState(bool state, string query) => Visible = CheckMatchesSearch(query) ? state : !state;
    }
}
