using Content.Shared._RedStar.Genetics.Data;

namespace Content.Client._RedStar.Genetics.UI;

/// <summary>
/// Colors and localization keys shared by the genetics interfaces.
/// </summary>
public static class GeneticsUi
{
    public static readonly Color PanelDark = Color.FromHex("#202832");
    public static readonly Color Panel = Color.FromHex("#252B35");
    public static readonly Color Border = Color.FromHex("#6A9BA6");
    public static readonly Color BarBackground = Color.FromHex("#171C24");
    public static readonly Color Accent = Color.FromHex("#85D5CA");
    public static readonly Color Warning = Color.FromHex("#E0B773");
    public static readonly Color Danger = Color.FromHex("#E08086");

    public static Color CategoryColor(GeneCategory category) => category switch
    {
        GeneCategory.Beneficial => Color.FromHex("#85D5AD"),
        GeneCategory.Pathological => Color.FromHex("#E58D98"),
        GeneCategory.Mixed => Color.FromHex("#E0BE7D"),
        GeneCategory.Cosmetic => Color.FromHex("#C0A5E0"),
        _ => Color.FromHex("#9ABBD1")
    };

    public static LocId CategoryLabel(GeneCategory category) => category switch
    {
        GeneCategory.Beneficial => "genetic-analyzer-category-beneficial",
        GeneCategory.Pathological => "genetic-analyzer-category-pathological",
        GeneCategory.Mixed => "genetic-analyzer-category-mixed",
        GeneCategory.Cosmetic => "genetic-analyzer-category-cosmetic",
        _ => "genetic-analyzer-category-neutral"
    };

    public static LocId ProcedureLabel(GeneticProcedure procedure) => procedure switch
    {
        GeneticProcedure.Activate => "genetics-console-activate",
        GeneticProcedure.Suppress => "genetics-console-suppress",
        _ => "genetics-console-remove"
    };

    public static LocId ProcedureProgress(GeneticProcedure procedure) => procedure switch
    {
        GeneticProcedure.Activate => "genetics-console-progress-activate",
        GeneticProcedure.Suppress => "genetics-console-progress-suppress",
        _ => "genetics-console-progress-remove"
    };
}
