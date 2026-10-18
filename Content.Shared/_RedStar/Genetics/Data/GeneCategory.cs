namespace Content.Shared._RedStar.Genetics.Data;

/// <summary>
/// Describes a gene's manifestations without changing its load or expression rules.
/// </summary>
public enum GeneCategory : byte
{
    Neutral,
    Beneficial,
    Pathological,
    Mixed,
    Cosmetic
}
