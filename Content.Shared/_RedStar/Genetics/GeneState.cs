namespace Content.Shared._RedStar.Genetics;

/// <summary>
/// The value of a present gene. Absence is represented by no genome entry.
/// </summary>
[DataDefinition]
public sealed partial class GeneState
{
    [DataField]
    public string? Allele;

    [DataField]
    public int? Number;

    public GeneState Clone() => new() { Allele = Allele, Number = Number };
}
