using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Prototypes;

/// <summary>
/// Defines the natural genes generated for an organism.
/// </summary>
[Prototype]
public sealed partial class GenomePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Natural genes present and active when this genome is generated.
    /// </summary>
    [DataField]
    public List<ProtoId<GenePrototype>> Genes = [];
}
