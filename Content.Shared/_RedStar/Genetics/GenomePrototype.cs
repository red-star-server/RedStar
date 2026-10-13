using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics;

/// <summary>
/// A genetic scheme shared by organisms of one compatible group.
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

    [DataField]
    public string? DefaultAllele;

    [DataField]
    public int? DefaultNumber;

    [DataField]
    public bool Randomize = true;
}
