using Content.Shared._RedStar.Genetics.Data;
using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared._RedStar.Genetics.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Components;

/// <summary>
/// Stores acquired modifications and a reference to the organism's donor catalogue. Only readiness is replicated.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(GenomeSystem))]
public sealed partial class GenomeComponent : Component
{
    [DataField(required: true)]
    public ProtoId<GenomePrototype> GenomeId;

    /// <summary>
    /// Set when the donor catalogue is initialized; required before sampling or introducing modifications.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool GenomeGenerated;

    /// <summary>
    /// Safe load capacity, represented as 100% on the analyzer. Acquired genes can exceed it.
    /// </summary>
    [DataField]
    public int MaximumLoad = 100;

    /// <summary>
    /// Acquired genes and their expression status. Catalogue entries do not activate effects or consume load.
    /// Mutations must go through ServerGenomeSystem to notify observers.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<GenePrototype>, GeneStatus> Genes = [];
}
