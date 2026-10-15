using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Tracks expressed values and independent incubation of acquired genes.
/// </summary>
[RegisterComponent, Access(typeof(GenomeExpressionSystem))]
public sealed partial class GenomeExpressionComponent : Component
{
    [DataField]
    public HashSet<ProtoId<GenePrototype>> Applied = [];

    [DataField]
    public Dictionary<ProtoId<GenePrototype>, IncubatingGene> Incubating = [];

    [DataField]
    public bool InitialGenomeExpressed;
}

/// <summary>
/// Holds the remaining game time for one acquired gene.
/// </summary>
[DataDefinition]
public sealed partial class IncubatingGene
{
    [DataField]
    public TimeSpan Remaining;

    public IncubatingGene() { }

    public IncubatingGene(TimeSpan remaining)
    {
        Remaining = remaining;
    }
}
