using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Raised after a genome transaction. Only these genes need expression.
/// </summary>
[ByRefEvent]
public readonly record struct GenomeChangedEvent(IReadOnlyList<ProtoId<GenePrototype>> Activated);

/// <summary>
/// Relayed to the entity containing an organism after its genome changes.
/// </summary>
[ByRefEvent]
public readonly record struct ContainedGenomeChangedEvent(EntityUid Subject);
