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

    [DataField]
    public new bool Initialized;

    [DataField]
    public Dictionary<ProtoId<GenePrototype>, GeneState> Genes = [];
}
