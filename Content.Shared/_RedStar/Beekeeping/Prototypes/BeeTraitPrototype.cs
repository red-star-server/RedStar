using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Beekeeping.Prototypes;

/// <summary>
/// A trait of a queen and her colony. Traits are independent and add up: multipliers multiply, flags combine.
/// Unstable mutagen injected into a queen rolls the pool of all traits: <see cref="Weight"/> to gain one she
/// lacks, <see cref="LossWeight"/> to lose one she has.
/// </summary>
[Prototype]
public sealed partial class BeeTraitPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Description;

    /// <summary>
    /// Colour of the trait's name in examine texts.
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// A bad trait: an unstable queen is likelier to gain it. Sting and aggression are tools, not bad.
    /// </summary>
    [DataField]
    public bool Bad;

    /// <summary>
    /// Weight of gaining this trait in a mutagen roll. Zero: never gained from mutagen.
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// Weight of losing this trait in a mutagen roll, if the queen has it. Zero: never lost to mutagen.
    /// </summary>
    [DataField]
    public float LossWeight = 1f;

    /// <summary>
    /// Innate traits take no slot.
    /// </summary>
    [DataField]
    public bool Innate;

    /// <summary>
    /// Multiplies how fast workers at home build combs.
    /// </summary>
    [DataField]
    public float CombRate = 1f;

    /// <summary>
    /// Multiplies how much nectar one comb costs.
    /// </summary>
    [DataField]
    public float CombCost = 1f;

    /// <summary>
    /// Multiplies the nectar one worker visit brings.
    /// </summary>
    [DataField]
    public float NectarPerVisit = 1f;

    /// <summary>
    /// Multiplies a pollinator's chance to double the next harvest.
    /// </summary>
    [DataField]
    public float PollinationChance = 1f;

    /// <summary>
    /// Added to a pollinator's pollinations. A pollinator always keeps at least one.
    /// </summary>
    [DataField]
    public int PollinatorCharges;

    /// <summary>
    /// Multiplies the nectar a new bee costs.
    /// </summary>
    [DataField]
    public float BeeCost = 1f;

    /// <summary>
    /// Multiplies how often the queen lays a new bee.
    /// </summary>
    [DataField]
    public float LayRate = 1f;

    /// <summary>
    /// Multiplies how long a bee taken out of the hive lives.
    /// </summary>
    [DataField]
    public float Lifetime = 1f;

    /// <summary>
    /// Multiplies the pause between the scout's flights.
    /// </summary>
    [DataField]
    public float ScoutPause = 1f;

    /// <summary>
    /// Multiplies how long the colony has to stay full before the queen lays an egg.
    /// </summary>
    [DataField]
    public float EggWait = 1f;

    /// <summary>
    /// Multiplies how long an egg lives before it dies uncollected.
    /// </summary>
    [DataField]
    public float EggLife = 1f;

    /// <summary>
    /// Multiplies the wait for the next egg after one is gone.
    /// </summary>
    [DataField]
    public float EggCooldown = 1f;

    /// <summary>
    /// Added to the trays the scout brings home from one flight (one without traits, at most five).
    /// </summary>
    [DataField]
    public int ScoutFinds;

    /// <summary>
    /// Added to the mutation level of a plant on each pollination.
    /// </summary>
    [DataField]
    public float PollinationMutation;

    /// <summary>
    /// A colony that lost this queen raises a new one by itself, given enough bees and nectar.
    /// </summary>
    [DataField]
    public bool EmergencyQueen;

    /// <summary>
    /// Pollination also fights pests and weeds in the tray.
    /// </summary>
    [DataField]
    public bool PestControl;

    /// <summary>
    /// The bees have a sting: reagents injected into the queen become their payload instead of being refused.
    /// </summary>
    [DataField]
    public bool Sting;

    /// <summary>
    /// The bees are aggressive.
    /// </summary>
    [DataField]
    public bool Aggressive;
}
