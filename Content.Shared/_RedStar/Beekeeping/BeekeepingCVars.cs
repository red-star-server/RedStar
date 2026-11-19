using Robust.Shared.Configuration;

namespace Content.Shared._RedStar.Beekeeping;

/// <summary>
/// Server limits that keep bees from becoming a weapon on a large scale. Admins change them with the cvar command.
/// </summary>
[CVarDefs]
public sealed class BeekeepingCVars
{
    /// <summary>
    /// Hives with a queen on one map. A queen put into one more hive does not take.
    /// </summary>
    public static readonly CVarDef<int> ActiveHiveLimit =
        CVarDef.Create("beekeeping.active_hive_limit", 8, CVar.SERVERONLY);

    /// <summary>
    /// Wild hives on one map. Past it an egg that dies uncollected just dies, and no swarm leaves.
    /// </summary>
    public static readonly CVarDef<int> WildHiveLimit =
        CVarDef.Create("beekeeping.wild_hive_limit", 3, CVar.SERVERONLY);

    /// <summary>
    /// Bees out of their hives (on errands or angry) within <see cref="AreaBeeRadius"/> of a hive. Past it the hive lets
    /// no more out: the colony keeps working, only fewer bees are seen.
    /// </summary>
    public static readonly CVarDef<int> AreaBeeLimit =
        CVarDef.Create("beekeeping.area_bee_limit", 20, CVar.SERVERONLY);

    /// <summary>
    /// Radius in tiles of <see cref="AreaBeeLimit"/>.
    /// </summary>
    public static readonly CVarDef<float> AreaBeeRadius =
        CVarDef.Create("beekeeping.area_bee_radius", 10f, CVar.SERVERONLY);
}
