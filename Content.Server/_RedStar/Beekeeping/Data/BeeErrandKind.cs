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
    /// A bee taken out of its hive and let go, flying back into it.
    /// </summary>
    Return
}
