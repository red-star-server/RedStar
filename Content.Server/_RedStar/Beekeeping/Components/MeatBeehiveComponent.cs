using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Beekeeping.Components;

/// <summary>
/// A hive that lives on blood: how its bees bite and hunt, how the colony rages, and what it has gathered. Added
/// when a hive turns to meat (see <see cref="MeatBeehiveSystem"/>).
/// </summary>
[RegisterComponent]
public sealed partial class MeatBeehiveComponent : Component
{
    /// <summary>
    /// Foragers of a meat hive that may be out biting at once.
    /// </summary>
    [DataField]
    public int MaxMeatForagers = 2;

    /// <summary>
    /// Blood one full bite takes from a creature.
    /// </summary>
    [DataField]
    public float BloodPerBite = 1f;

    /// <summary>
    /// What a bite does to a living creature.
    /// </summary>
    [DataField]
    public DamageSpecifier MeatBiteDamage = new() { DamageDict = { ["Piercing"] = 1 } };

    /// <summary>
    /// Each bite on the same creature within this long gives half of the one before, down to
    /// <see cref="BiteMinFactor"/> of a full one. A new window starts after it.
    /// </summary>
    [DataField]
    public TimeSpan BiteFalloffWindow = TimeSpan.FromMinutes(5);

    [DataField]
    public float BiteMinFactor = 0.25f;

    /// <summary>
    /// The same creature takes the next bite no sooner than this.
    /// </summary>
    [DataField]
    public TimeSpan BiteInterval = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The bite's sound: quiet, with a short range, but it can give the hive away to someone close.
    /// </summary>
    [DataField]
    public SoundSpecifier MeatBiteSound = new SoundPathSpecifier("/Audio/Effects/bite.ogg", AudioParams.Default.WithVolume(-5f).WithMaxDistance(9f));

    /// <summary>
    /// Added to <see cref="Meat"/> for each bite on a dead creature.
    /// </summary>
    [DataField]
    public int MeatPerDeadBite = 1;

    /// <summary>
    /// Most blood the hive keeps in stock; the rest is lost.
    /// </summary>
    [DataField]
    public float BloodStockCap = 60f;

    /// <summary>
    /// The visible forager of a meat colony: unlike the usual one it can be hit and killed.
    /// </summary>
    [DataField]
    public EntProtoId MeatForagerPrototype = "BeeForagerMeat";

    /// <summary>
    /// A meat forager reaches a target through the pipes when it has no straight way: it comes out of a vent of the
    /// hive's network that is this near the target, in tiles, and in clear sight of it.
    /// </summary>
    [DataField]
    public float VentHuntRange = 8f;

    /// <summary>
    /// Vents farther from the hive than this are not looked at for targets: it bounds the search on a big station.
    /// </summary>
    [DataField]
    public float VentHuntReach = 40f;

    /// <summary>
    /// How fast a bee goes through the pipes, in tiles per second.
    /// </summary>
    [DataField]
    public float VentPipeSpeed = 3f;

    /// <summary>
    /// A meat colony turns furious for this long after one of its foragers is killed, and again with each kill.
    /// </summary>
    [DataField]
    public TimeSpan RageDuration = TimeSpan.FromMinutes(3);

    /// <summary>
    /// The least rage a provoked colony has, from 0 to 1. The fewer bees are left, the nearer to 1 it gets.
    /// </summary>
    [DataField]
    public float RageMin = 0.25f;

    /// <summary>
    /// What full rage does: foragers fly this much faster (1 doubles the speed), this many more foragers are out at
    /// once, and whoever killed a bee is this many times likelier to be the next target.
    /// </summary>
    [DataField]
    public float RageSpeedBonus = 1f;

    [DataField]
    public int RageExtraForagers = 2;

    [DataField]
    public float RageAttackerBias = 6f;

    /// <summary>
    /// How many bites the colony sends at the same killer, no more: revenge is one or two attacks, not a hunt.
    /// </summary>
    [DataField]
    public int RageRevenge = 2;

    /// <summary>
    /// With this many bees or fewer left the colony no longer sends them after a killer: the last ones stay safe.
    /// </summary>
    [DataField]
    public int RageRevengeMinBees = 3;

    /// <summary>
    /// The colony is furious until this time. Server only.
    /// </summary>
    [ViewVariables]
    public TimeSpan RageUntil;

    /// <summary>
    /// Whoever killed one of its bees lately, and until when it remembers. Server only.
    /// </summary>
    [ViewVariables]
    public Dictionary<EntityUid, (TimeSpan Until, int Left)> Attackers = new();

    /// <summary>
    /// The blood a meat colony's bees look like before any has been brought.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> DefaultBlood = "Blood";

    /// <summary>
    /// Blood each new comb of a meat hive takes from the stock.
    /// </summary>
    [DataField]
    public float CombBlood = 2f;

    /// <summary>
    /// The reagent each comb of a broken meat hive also carries, and how much: blood does not heal most eaters, this
    /// does (it brings the blood level back).
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> CombHealReagent = "Protein";

    [DataField]
    public float CombHeal = 2f;

    /// <summary>
    /// Pieces of meat a broken meat hive always gives, one more for every <see cref="MeatPerDrop"/> of
    /// <see cref="Meat"/>, and no more than <see cref="MeatDropCap"/> in all.
    /// </summary>
    [DataField]
    public int MeatBase = 1;

    [DataField]
    public int MeatPerDrop = 3;

    [DataField]
    public int MeatDropCap = 10;

    /// <summary>
    /// Blood already put into the combs. Server only.
    /// </summary>
    [ViewVariables]
    public Solution CombBlooded = new();

    /// <summary>
    /// The blood the bees brought, by reagent. Server only.
    /// </summary>
    [ViewVariables]
    public Solution Blood = new();

    /// <summary>
    /// Every kind of blood this colony has ever brought. Server only.
    /// </summary>
    [ViewVariables]
    public List<ProtoId<ReagentPrototype>> BloodTypes = new();

    /// <summary>
    /// Grows with each bite on a dead creature; the more, the more meat falls out of a broken meat hive. Server only.
    /// </summary>
    [ViewVariables]
    public int Meat;
}
