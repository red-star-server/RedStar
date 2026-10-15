using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Raised after a genome transaction. Only these genes need expression.
/// </summary>
public sealed class GenomeChangedEvent(IReadOnlyList<ProtoId<GenePrototype>> activated) : EntityEventArgs
{
    public readonly IReadOnlyList<ProtoId<GenePrototype>> Activated = activated;
}
