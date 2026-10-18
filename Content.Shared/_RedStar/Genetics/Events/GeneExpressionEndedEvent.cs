using Content.Shared._RedStar.Genetics.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Events;

/// <summary>
/// Raised on the host after a persistent gene manifestation and its contributions have been removed.
/// </summary>
[ByRefEvent]
public readonly record struct GeneExpressionEndedEvent(ProtoId<GenePrototype> Gene);
