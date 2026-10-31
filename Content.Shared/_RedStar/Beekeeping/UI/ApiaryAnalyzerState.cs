using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.UI;

/// <summary>
/// What the apiary analyzer shows. A hive fills the colony part, a queen or a bee only the traits and its own lines.
/// No timers: admins read those in View Variables.
/// </summary>
[Serializable, NetSerializable]
public sealed class ApiaryAnalyzerState : BoundUserInterfaceState
{
    public NetEntity Target;
    public ApiaryScanKind Kind;

    /// <summary>
    /// The traits of the queen (or of the hive's queen) or of the bee.
    /// </summary>
    public List<ProtoId<BeeTraitPrototype>> Traits = new();

    public BeeTraitTotals Totals = new();

    /// <summary>
    /// A queen is known: the scanned one or the one in the hive.
    /// </summary>
    public bool HasQueen;

    public int UsedSlots;
    public int Instability;
    public BeehiveDiet Diet;

    /// <summary>
    /// A meat hive: pieces of meat that would fall out if it were broken now, the most it gives, the bites on the dead
    /// that add one more piece, the chance of each kind and what the hive holds.
    /// </summary>
    public int MeatDrops;
    public int MeatMax;
    public int MeatBase;
    public int MeatPerDrop;
    public List<ApiaryMeatChance> MeatChances = new();
    public List<ApiaryReagentData> Chemicals = new();
    public int Slots;
    public float Payload;
    public float PayloadCap;

    #region Hive

    public bool Wild;
    public int Bees;
    public int OutBees;
    public int MaxBees;
    public float Nectar;
    public float MaxNectar;
    public ApiaryNectarUse NectarUse;

    /// <summary>
    /// Rough seconds until the queen makes the next bee; negative when it cannot be told (full colony, no queen, no
    /// workers at home, the hive paused).
    /// </summary>
    public bool BroodGoing;

    /// <summary>
    /// Workers flying to the trays and back right now.
    /// </summary>
    public int Foraging;
    public int Combs;
    public int MaxCombs;
    public float Health;
    public float MaxHealth;
    public bool HasEgg;
    public List<ApiaryFrameData> Frames = new();
    public List<ApiaryTrayData> Trays = new();

    #endregion

    #region Bee

    public int Charges;

    /// <summary>
    /// The scout's own hive still stands, so it has somewhere to bring its trays.
    /// </summary>
    public bool HiveKnown;

    public int Memory;
    public int MemoryLimit;

    #endregion
}
