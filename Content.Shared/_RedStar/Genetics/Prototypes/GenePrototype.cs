using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Content.Shared._RedStar.Genetics.Data;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Prototypes;

[Prototype]
public sealed partial class GenePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Research tier used to group genes in the catalogue and mutation recipes.
    /// </summary>
    [DataField]
    public int Tier = 1;

    /// <summary>
    /// Load reserved by an acquired gene, including while dormant or suppressed.
    /// Natural genes do not consume acquired load.
    /// </summary>
    [DataField]
    public int LoadCost
    {
        get;
        private set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    } = 1;

    /// <summary>
    /// Describes the gene's manifestations independently of its research tier and load.
    /// A mixed gene combines useful and harmful effects in the same effect list.
    /// </summary>
    [DataField]
    public GeneCategory Category = GeneCategory.Neutral;

    /// <summary>
    /// Organisms allowed to receive or express this gene. Null permits any organism.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Organisms forbidden from receiving or expressing this gene, even if whitelisted.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

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
