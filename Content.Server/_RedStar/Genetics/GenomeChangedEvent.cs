namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Phenotype systems can react to genetic changes without owning genetic data.
/// </summary>
public sealed class GenomeChangedEvent : EntityEventArgs;

/// <summary>
/// A restored genome is available when its entity enters the map.
/// </summary>
public sealed class GenomeReadyEvent : EntityEventArgs;

public enum GenomeOperationResult : byte
{
    Success,
    MissingGenome,
    UnknownScheme,
    UnknownGene,
    InvalidValue,
    IncompatibleScheme,
    Uninitialized
}
