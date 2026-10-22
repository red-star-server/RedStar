using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Prototypes;

/// <summary>
/// Defines the donor catalogue sampled from an organism, independently of its working components.
/// </summary>
[Prototype]
public sealed partial class GenomePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Genes available in a biopsy. These entries do not apply effects to the donor.
    /// </summary>
    [DataField]
    public List<ProtoId<GenePrototype>> Genes = [];
}
