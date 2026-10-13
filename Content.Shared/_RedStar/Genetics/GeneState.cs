namespace Content.Shared._RedStar.Genetics;

/// <summary>
/// The inherited state of a gene. Expression is decided by another system.
/// </summary>
[DataDefinition]
public sealed partial class GeneState
{
    [DataField]
    public bool Present = true;

    [DataField]
    public string? Allele;

    [DataField]
    public int? Number;

    public GeneState Clone() => new() { Present = Present, Allele = Allele, Number = Number };
}
