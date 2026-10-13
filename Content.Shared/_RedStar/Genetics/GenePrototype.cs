using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics;

/// <summary>
/// Describes the value carried by a present gene; absence is stored in the genome itself.
/// </summary>
public enum GeneKind : byte
{
    Presence,
    Allele,
    Number
}

/// <summary>
/// Defines one inherited value, independently of its eventual phenotype.
/// </summary>
[Prototype]
public sealed partial class GenePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public GeneKind Kind;

    [DataField]
    public List<string> Alleles = [];

    [DataField]
    public int Minimum;

    [DataField]
    public int Maximum;
}
