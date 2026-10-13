using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

public enum GenomeChangeKind : byte
{
    Added,
    Removed,
    Changed,
    Replaced
}

/// <summary>
/// Phenotype systems can react to genetic changes without owning genetic data.
/// </summary>
public sealed class GenomeChangedEvent(
    GenomeChangeKind kind,
    ProtoId<GenePrototype>? gene = null) : EntityEventArgs
{
    public GenomeChangeKind Kind { get; } = kind;
    public ProtoId<GenePrototype>? Gene { get; } = gene;
}

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
    MissingGene,
    InvalidValue,
    IncompatibleScheme,
    Uninitialized
}
