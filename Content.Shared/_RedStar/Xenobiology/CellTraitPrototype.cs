using Robust.Shared.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared._RedStar.Genetics;

namespace Content.Shared._RedStar.Xenobiology;

/// <summary>
/// Describes a biological cell trait and the permanent effects applied when it is gained.
/// </summary>
[Prototype]
public sealed partial class CellTraitPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// A gene whose presence makes this phenotype eligible for expression.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<GenePrototype> Gene;

    /// <summary>
    /// Optional allele required in addition to the gene's presence.
    /// </summary>
    [DataField]
    public string? Allele;

    [DataField]
    public LocId Name;

    [DataField]
    public Color Color;

    [DataField]
    public EntityEffect[] Effects = [];
}
