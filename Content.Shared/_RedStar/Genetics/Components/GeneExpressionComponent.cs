using Content.Shared._RedStar.Genetics.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Components;

/// <summary>
/// Identifies a gene's persistent status effect and the resources it grants to its host.
/// Components already present on the host retain their configuration.
/// </summary>
[RegisterComponent]
public sealed partial class GeneExpressionComponent : Component
{
    [DataField(required: true)]
    public ProtoId<GenePrototype> Gene;

    /// <summary>
    /// Host components required by this manifestation, shared with other gene manifestations.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Host actions required by this manifestation. An existing action of the same prototype is retained.
    /// </summary>
    [DataField]
    public List<EntProtoId> Actions = new();

    /// <summary>
    /// Named host solutions required by this manifestation. Existing solutions are preserved.
    /// </summary>
    [DataField]
    public Dictionary<string, EntProtoId> Solutions = new();
}
