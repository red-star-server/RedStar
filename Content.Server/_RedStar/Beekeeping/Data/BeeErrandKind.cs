namespace Content.Server._RedStar.Beekeeping.Data;

public enum BeeErrandKind : byte
{
    /// <summary>
    /// A worker showing a visit to a tray. It can be caught by hand.
    /// </summary>
    Forage,

    /// <summary>
    /// The scout flying to the first tray it found; the hive learns all of them when it lands.
    /// </summary>
    Scout,

    /// <summary>
    /// A meat colony's worker going for a creature.
    /// </summary>
    Hunt,

    /// <summary>
    /// A swarm or a lone queen on the way to a new home. It does not come back.
    /// </summary>
    Swarm,

    /// <summary>
    /// An angry bee whose time is up, flying back into its hive. Its count is the angry hive's.
    /// </summary>
    Return,
}
