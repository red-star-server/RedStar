using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Guidebook.Controls;
using Content.Client.Guidebook.Richtext;
using Content.Client.Message;
using Content.Client.UserInterface.ControlExtensions;
using Content.Shared._RedStar.Genetics.Prototypes;
using JetBrains.Annotations;
using Robust.Client.Graphics;
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

    private sealed class GeneEntry : PanelContainer, ISearchableControl, IPrototypeRepresentationControl
    {
        public IPrototype? RepresentedPrototype { get; }

        public GeneEntry(GenePrototype gene)
        {
            RepresentedPrototype = gene;
            Margin = new Thickness(5);
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#2b2e31"),
                BorderColor = Color.FromHex("#777777"),
                BorderThickness = new Thickness(1),
            };
            var body = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Margin = new Thickness(10, 8),
                SeparationOverride = 6,
            };
            AddChild(body);
            var header = new BoxContainer { SeparationOverride = 10 };
            body.AddChild(header);
            var name = new RichTextLabel { HorizontalExpand = true };
            name.SetMarkup($"[bold]{Loc.GetString($"gene-{gene.ID}-name")}[/bold]");
            header.AddChild(name);
            header.AddChild(new Label
            {
                Text = Loc.GetString("guidebook-genetic-mutation-tier", ("tier", gene.Tier)),
            });
            body.AddChild(new Label
            {
                Text = Loc.GetString("guidebook-gene-properties", ("load", gene.LoadCost),
                    ("category", Loc.GetString($"guidebook-gene-category-{gene.Category.ToString().ToLowerInvariant()}"))),
            });
            var description = new RichTextLabel();
            description.SetMessage(Loc.GetString($"gene-{gene.ID}-desc"));
            body.AddChild(description);
        }

        public bool CheckMatchesSearch(string query) => this.ChildrenContainText(query);

        public void SetHiddenState(bool state, string query) => Visible = CheckMatchesSearch(query) ? state : !state;
    }
}
