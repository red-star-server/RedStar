using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics;

[Prototype]
public sealed partial class GenePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Acquired-gene load consumed by this gene; natural genes do not consume load.
    /// </summary>
    [DataField]
    public int Tier = 1;

    /// <summary>
    /// Number of overlapping DNA fragments in the sequencing puzzle.
    /// </summary>
    [DataField]
    public int FragmentCount = 4;

    /// <summary>
    /// Minimum sample stability, from zero to one, required to extract this gene.
    /// </summary>
    [DataField]
    public float MinimumStability = 0.5f;

    /// <summary>
    /// Effects attempted when the gene is activated. An empty list still permits activation.
    /// </summary>
    [DataField]
    public EntityEffect[] Effects = [];
}
