using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Server-owned inherited data. Use <see cref="GenomeSystem"/> to access it.
/// </summary>
[RegisterComponent, Access(typeof(GenomeSystem))]
public sealed partial class GenomeComponent : Component
{
    [DataField(required: true)]
    public ProtoId<GenomePrototype> GenomeId;

    /// <summary>
    /// Prevents generation from overwriting a loaded or copied genome.
    /// </summary>
    [DataField]
    public bool GenomeGenerated;

    [DataField]
    public Dictionary<ProtoId<GenePrototype>, GeneState> Genes = [];
}
