using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics;

/// <summary>
/// Defines the natural genes generated for an organism.
/// </summary>
[Prototype]
public sealed partial class GenomePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public List<GenomeGeneDefinition> Genes = [];
}

[DataDefinition]
public sealed partial class GenomeGeneDefinition
{
    [DataField(required: true)]
    public ProtoId<GenePrototype> Gene;

    /// <summary>
    /// Chance that an optional gene is present in a newly generated genome.
    /// </summary>
    [DataField]
    public float PresenceChance = 1f;
}
