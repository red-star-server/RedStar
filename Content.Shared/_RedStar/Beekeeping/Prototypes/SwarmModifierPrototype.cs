using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Beekeeping.Prototypes;

/// <summary>
/// A quirk of one swarm, rolled once when it leaves the hive by <see cref="Weight"/>, not tied to the queen's traits.
/// The wild hive the swarm builds keeps it and shows it when examined.
/// </summary>
[Prototype]
public sealed partial class SwarmModifierPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Description;

    /// <summary>
    /// Colour of the name in the examine text: green for good, red for bad, grey for plain.
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// The share of the colony that leaves with the swarm.
    /// </summary>
    [DataField]
    public float ColonyShare = 0.5f;

    /// <summary>
    /// The swarm does not look for vents and nests near the hive, as before.
    /// </summary>
    [DataField]
    public bool SkipVents;

    /// <summary>
    /// Multiplies how far the swarm looks for vents and nests.
    /// </summary>
    [DataField]
    public float RangeMultiplier = 1f;

    /// <summary>
    /// Combs the wild hive holds; zero keeps the hive's own limit.
    /// </summary>
    [DataField]
    public int WildCombs;

    /// <summary>
    /// When the wild hive is broken, the combs get a random reagent from this list.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomFillSolutionPrototype>? CombReagent;
}
