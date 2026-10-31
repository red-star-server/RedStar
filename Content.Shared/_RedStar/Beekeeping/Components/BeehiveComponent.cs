using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Guidebook;
using Content.Shared.Whitelist;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Beekeeping.Components;

/// <summary>
/// A beehive: holds a queen and frames in item slots and keeps an abstract colony. The bees are a counter, not
/// entities. Nothing here is networked: the colony runs on the server, and the examine text and the analyzer window
/// are filled there. The client only needs the slots (for the menus) and the appearance data.
/// All tuning lives here so it can be changed in the prototype or with View Variables.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class BeehiveComponent : Component
{
    #region Slots

    /// <summary>
    /// Item slot that holds the queen.
    /// </summary>
    [DataField]
    public string QueenSlot = "queen";

    /// <summary>
    /// Item slots that hold frames, in the order they are filled and numbered on the sprite.
    /// </summary>
    [DataField]
    public List<string> FrameSlots = new() { "frame1", "frame2", "frame3", "frame4" };

    #endregion

    #region Colony

    /// <summary>
    /// Bees at home, the scout included while it is at home.
    /// </summary>
    [DataField]
    public int Bees;

    /// <summary>
    /// Bees of this hive that are outside, as items or angry bees. They count against <see cref="MaxBees"/> but do
    /// no work.
    /// </summary>
    [DataField]
    public int OutBees;

    #region Emergency queen

    /// <summary>
    /// The traits of the colony's last queen. A colony whose lost queen had an emergency-queen trait raises a new
    /// one.
    /// </summary>
    [ViewVariables]
    public List<ProtoId<BeeTraitPrototype>> ColonyTraits = new();

    /// <summary>
    /// How long a queenless colony with the trait takes to raise a new queen.
    /// </summary>
    [DataField]
    public TimeSpan EmergencyQueenTime = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Bees and nectar the colony needs for that, and the nectar it spends.
    /// </summary>
    [DataField]
    public int EmergencyQueenBees = 3;

    [DataField]
    public float EmergencyQueenNectar = 20f;

    /// <summary>
    /// Since when the colony has been raising a new queen.
    /// </summary>
    [ViewVariables]
    public TimeSpan? EmergencyQueenSince;

    #endregion

    #region Queen egg

    /// <summary>
    /// A second queen's egg lies in the hive. Royal jelly can be collected from it; left alone, it dies.
    /// </summary>
    [DataField]
    public bool HasEgg;

    /// <summary>
    /// When the egg dies if nobody collects the jelly.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan EggDiesAt;

    /// <summary>
    /// The colony has to stay full this long for an egg to appear. Bees in hands and angry bees outside count.
    /// </summary>
    [DataField, GuidebookData]
    public TimeSpan EggFullTime = TimeSpan.FromMinutes(3);

    /// <summary>
    /// How long an egg lives.
    /// </summary>
    [DataField, GuidebookData]
    public TimeSpan EggLifetime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// No new egg for this long after one is collected or dies.
    /// </summary>
    [DataField, GuidebookData]
    public TimeSpan EggCooldown = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The examine text warns this long before the egg dies.
    /// </summary>
    [DataField]
    public TimeSpan EggDyingWarning = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Since when the colony has been full with enough nectar.
    /// </summary>
    [ViewVariables]
    public TimeSpan? EggFullSince;

    /// <summary>
    /// No egg before this time.
    /// </summary>
    [ViewVariables]
    public TimeSpan EggCooldownUntil;

    [DataField]
    public ProtoId<ReagentPrototype> JellyReagent = "RoyalJelly";

    /// <summary>
    /// Royal jelly one egg gives.
    /// </summary>
    [DataField, GuidebookData]
    public float JellyAmount = 5f;

    /// <summary>
    /// Seconds collecting the jelly takes.
    /// </summary>
    [DataField]
    public float JellyDelay = 5f;

    #endregion

    #region Swarm and wild hive

    /// <summary>
    /// A wild hive built by a swarm: no frames, a fixed bee limit, combs kept inside. Taken only by breaking it.
    /// </summary>
    [DataField]
    public bool Wild;

    /// <summary>
    /// Bee limit of a wild hive.
    /// </summary>
    [DataField]
    public int WildMaxBees = 15;

    /// <summary>
    /// Combs inside a wild hive. They fall out when it is broken. Server only.
    /// </summary>
    [DataField]
    public int WildCombs;

    /// <summary>
    /// Most combs a wild hive holds.
    /// </summary>
    [DataField]
    public int MaxWildCombs = 8;

    /// <summary>
    /// The quirk of the swarm that built this wild hive, shown when it is examined.
    /// </summary>
    [DataField]
    public ProtoId<SwarmModifierPrototype>? SwarmModifier;

    /// <summary>
    /// The honey shows on a wild hive from this share of its combs.
    /// </summary>
    [DataField]
    public float WildHoneyShown = 0.5f;

    /// <summary>
    /// Fewest bees at home for a swarm to leave when an egg dies.
    /// </summary>
    [DataField]
    public int SwarmMinBees = 4;

    /// <summary>
    /// How far a swarm looks for a new home, in tiles.
    /// </summary>
    [DataField]
    public float SwarmRange = 12f;

    [DataField]
    public EntProtoId SwarmPrototype = "BeeSwarm";

    /// <summary>
    /// The bees that leave a crowded hive together, with no queen.
    /// </summary>
    [DataField]
    public EntProtoId LeavingSwarmPrototype = "BeeSwarmLeaving";

    [DataField]
    public EntProtoId WildHivePrototype = "WildBeehive";

    /// <summary>
    /// What one comb of a wild hive becomes when the hive is broken.
    /// </summary>
    [DataField]
    public EntProtoId WildCombPrototype = "FoodHoneycomb";

    /// <summary>
    /// The young queen hatched from the egg, left behind in the hive when the old one swarms out.
    /// </summary>
    [DataField]
    public EntProtoId DaughterQueenPrototype = "BeeQueen";

    #endregion

    /// <summary>
    /// Payload each bee of a colony with a sting takes from the queen when it leaves the hive.
    /// </summary>
    [DataField]
    public float PayloadPerBee = 2f;

    /// <summary>
    /// Population limit: <see cref="BeesPerFrame"/> times the number of inserted frames.
    /// </summary>
    [DataField]
    public int MaxBees;

    /// <summary>
    /// Why the bees are not working right now, if they are not.
    /// </summary>
    [DataField]
    public BeehivePauseReason Pause;

    /// <summary>
    /// Where the hive's only scout is.
    /// </summary>
    [DataField]
    public BeeScoutState ScoutState;

    /// <summary>
    /// When a lost scout may be replaced by a worker. Only meaningful while <see cref="ScoutState"/> is
    /// <see cref="BeeScoutState.Lost"/>.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan ScoutReplaceAt;

    /// <summary>
    /// Whether the hive has more bees than its frames allow.
    /// </summary>
    [DataField]
    public BeehiveCrowding Crowding;

    #endregion

    #region Population

    /// <summary>
    /// Bees a hive can hold per inserted frame.
    /// </summary>
    [DataField, GuidebookData]
    public int BeesPerFrame = 5;

    /// <summary>
    /// Bees a colony starts with when a queen is put into a truly empty hive: one scout and the rest workers.
    /// </summary>
    [DataField]
    public int StartingBees = 5;

    /// <summary>
    /// How many of the bees at home scout instead of carrying nectar.
    /// </summary>
    [DataField]
    public int Scouts = 1;

    /// <summary>
    /// How many bees of this hive may be outside as items at once.
    /// </summary>
    [DataField, GuidebookData]
    public int MaxOutBees = 3;

    /// <summary>
    /// Nectar one new bee costs.
    /// </summary>
    [DataField]
    public float BeeCost = 5f;

    /// <summary>
    /// The queen lays at most one new bee this often. Nectar the colony cannot turn into bees meanwhile goes into
    /// combs, so a hive with all its frames in makes honey while it grows.
    /// </summary>
    [DataField]
    public TimeSpan LayInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    /// When the queen may lay the next bee.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextLay;

    /// <summary>
    /// Without a queen, one bee dies this often.
    /// </summary>
    [DataField]
    public TimeSpan QueenlessDecayInterval = TimeSpan.FromMinutes(2);

    /// <summary>
    /// How long an overcrowded hive waits for its frames to come back before bees start to leave.
    /// </summary>
    [DataField]
    public TimeSpan CrowdingGrace = TimeSpan.FromMinutes(5);

    /// <summary>
    /// After the grace period, one extra bee leaves this often.
    /// </summary>
    [DataField]
    public TimeSpan CrowdingLeaveInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How long after its death a lost scout is replaced by a worker.
    /// </summary>
    [DataField]
    public TimeSpan ScoutReplaceDelay = TimeSpan.FromMinutes(3);

    /// <summary>
    /// A lost scout is only replaced while at least this many bees are at home.
    /// </summary>
    [DataField]
    public int ScoutReplaceMinBees = 2;

    #endregion

    #region Production

    /// <summary>
    /// Largest nectar stock.
    /// </summary>
    [DataField]
    public float MaxNectar = 100f;

    /// <summary>
    /// Nectar one comb costs.
    /// </summary>
    [DataField]
    public float CombCost = 10f;

    /// <summary>
    /// Combs one worker at home makes per minute while there is nectar for them.
    /// </summary>
    [DataField]
    public float CombsPerWorkerPerMinute = 0.1f;

    /// <summary>
    /// How often one worker flies out for nectar.
    /// </summary>
    [DataField]
    public TimeSpan VisitInterval = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Nectar one visit brings.
    /// </summary>
    [DataField]
    public float NectarPerVisit = 1f;

    /// <summary>
    /// Nectar stock at or above these fractions of <see cref="MaxNectar"/> reads as low, medium and high.
    /// </summary>
    [DataField]
    public float NectarLowFraction = 0.01f;

    [DataField]
    public float NectarMediumFraction = 0.3f;

    [DataField]
    public float NectarHighFraction = 0.7f;

    #endregion

    #region Nectar sources

    /// <summary>
    /// What counts as a nectar source when scouting, on top of having a living plant. Null means any plant
    /// holder (hydroponics tray or soil). Kept in data so other forage types can be added later.
    /// </summary>
    [DataField]
    public EntityWhitelist? SourceWhitelist;

    /// <summary>
    /// Shortest and longest automatic scouting trip.
    /// </summary>
    [DataField]
    public TimeSpan ScoutTripMin = TimeSpan.FromSeconds(60);

    [DataField]
    public TimeSpan ScoutTripMax = TimeSpan.FromSeconds(120);

    /// <summary>
    /// How far from the hive, in tiles, the scout finds trays.
    /// </summary>
    [DataField]
    public float ScoutRange = 8f;

    /// <summary>
    /// New trays one scouting trip brings home, before the queen's traits.
    /// </summary>
    [DataField]
    public int ScoutFinds = 1;

    /// <summary>
    /// Bees only fly to known places at most this many tiles from where the hive is now. Farther places stay
    /// remembered but inactive until the hive comes back closer.
    /// </summary>
    [DataField]
    public float VisitRange = 12f;

    /// <summary>
    /// How many places the hive remembers.
    /// </summary>
    [DataField]
    public int MaxKnownSources = 8;

    /// <summary>
    /// How often an inactive place is checked again.
    /// </summary>
    [DataField]
    public TimeSpan InactiveRecheck = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Visible forager bees one hive may have in the air at once. Purely visual.
    /// </summary>
    [DataField]
    public int MaxForagers = 3;

    /// <summary>
    /// The scout in the air right now, if it is flying.
    /// </summary>
    [ViewVariables]
    public EntityUid? ScoutFlyer;

    #endregion

    #region Bees in hand

    /// <summary>
    /// The scout, flying out to the trays it found or taken out by hand.
    /// </summary>
    [DataField]
    public EntProtoId ScoutPrototype = "BeeScout";

    /// <summary>
    /// A worker, flying out to a tray or taken out by hand.
    /// </summary>
    [DataField]
    public EntProtoId PollinatorPrototype = "BeePollinator";

    /// <summary>
    /// Seconds taking a bee out takes.
    /// </summary>
    [DataField]
    public float TakeDelay = 2f;

    /// <summary>
    /// How long a bee that worked outside rests at home before it can be taken out again. Without it, bringing a
    /// pollinator back and taking it again would give fresh pollinations for free.
    /// </summary>
    [DataField]
    public TimeSpan WorkedRest = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When each resting worker is ready again.
    /// </summary>
    [ViewVariables]
    public List<TimeSpan> RestingWorkers = new();

    /// <summary>
    /// The scout rests until then after a trip in someone's hands.
    /// </summary>
    [ViewVariables]
    public TimeSpan ScoutRestsUntil;

    /// <summary>
    /// Hidden look-alikes of a scout and a worker of this hive, kept in containers of the hive. The context
    /// menu shows them as live icons, so the icons follow the colour of the colony.
    /// </summary>
    [DataField]
    public EntProtoId ScoutIconPrototype = "BeeScoutIcon";

    [DataField]
    public EntProtoId PollinatorIconPrototype = "BeePollinatorIcon";

    [DataField]
    public string ScoutIconContainer = "scout_icon";

    [DataField]
    public string PollinatorIconContainer = "pollinator_icon";

    #endregion

    #region Air

    /// <summary>
    /// Bees stop working below this pressure, in kPa.
    /// </summary>
    [DataField]
    public float MinPressure = Atmospherics.WarningLowPressure;

    /// <summary>
    /// Bees stop working above this pressure, in kPa.
    /// </summary>
    [DataField]
    public float MaxPressure = Atmospherics.WarningHighPressure;

    /// <summary>
    /// Bees stop working below this temperature, in K.
    /// </summary>
    [DataField]
    public float MinTemperature = 273.15f;

    /// <summary>
    /// Bees stop working above this temperature, in K.
    /// </summary>
    [DataField]
    public float MaxTemperature = 323.15f;

    /// <summary>
    /// Water vapor on the hive's tile, in moles, from which its bees are calm: no angry bees come out. They keep working.
    /// </summary>
    [DataField]
    public float CalmVaporMoles = 1f;

    #endregion

    #region Server state

    /// <summary>
    /// Nectar in stock. Editable in View Variables for testing.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Nectar;

    /// <summary>
    /// Nectar already put into the next bee.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float BeeProgress;

    /// <summary>
    /// Part of the next comb already made.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float CombProgress;

    /// <summary>
    /// When the hive is updated next.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// Game time of the previous update, to scale the work done by the time that passed.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan LastUpdate;

    /// <summary>
    /// When the queenless colony loses its next bee.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextQueenlessDecay;

    /// <summary>
    /// When the hive became overcrowded, or null if it is not.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan? CrowdedSince;

    /// <summary>
    /// When the next extra bee leaves an overcrowded hive.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextCrowdingLeave;

    /// <summary>
    /// Trays the hive knows. The hive remembers the tray itself: unbolted and moved, it is still found.
    /// </summary>
    [ViewVariables]
    public List<BeehiveSource> KnownSources = new();

    /// <summary>
    /// Worker visits that are due but not made yet. Never banks more than one: idle workers do not save up.
    /// </summary>
    [ViewVariables]
    public float VisitBudget;

    /// <summary>
    /// When the current automatic scouting trip ends.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextScoutTrip;

    /// <summary>
    /// A colony that has just started sends its scout out after <see cref="FirstScoutDelay"/> instead of the usual
    /// wait before the first trip.
    /// </summary>
    [ViewVariables]
    public bool ScoutFirstTripNow;

    [DataField]
    public TimeSpan FirstScoutDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Visible forager bees of this hive that are in the air.
    /// </summary>
    [ViewVariables]
    public List<EntityUid> Foragers = new();

    /// <summary>
    /// Visible foragers still to fly out, with the time each one leaves.
    /// </summary>
    [ViewVariables]
    public List<(EntityUid Tray, TimeSpan At)> PendingForagers = new();

    /// <summary>
    /// A forager or the scout hit by someone: the hive sends angry bees after them, at most this often.
    /// </summary>
    [DataField]
    public TimeSpan DefenseCooldown = TimeSpan.FromSeconds(5);

    [ViewVariables]
    public TimeSpan NextDefense;

    /// <summary>
    /// Visits made at the same time are shown spread over this long.
    /// </summary>
    [DataField]
    public TimeSpan ForagerSpread = TimeSpan.FromSeconds(20);

    #endregion
}
