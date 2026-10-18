using Content.Shared._RedStar.Genetics.Data;
using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared._RedStar.Genetics.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Components;

/// <summary>
/// Stores inherited and acquired genes on the server. Only generation readiness is replicated.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(GenomeSystem))]
public sealed partial class GenomeComponent : Component
{
    [DataField(required: true)]
    public ProtoId<GenomePrototype> GenomeId;

    /// <summary>
    /// Set after generation or loading; prevents MapInit from replacing an existing genome.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool GenomeGenerated;

    /// <summary>
    /// Maximum combined load cost of acquired genes, including dormant and suppressed genes.
    /// </summary>
    [DataField]
    public int MaximumLoad = 6;

    /// <summary>
    /// Genes inherited from the original genome, excluded from acquired-gene load.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<GenePrototype>> NaturalGenes = [];

    /// <summary>
    /// All present genes and their expression status, including dormant and suppressed genes.
    /// Mutations must go through ServerGenomeSystem to notify observers.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<GenePrototype>, GeneStatus> Genes = [];
}
