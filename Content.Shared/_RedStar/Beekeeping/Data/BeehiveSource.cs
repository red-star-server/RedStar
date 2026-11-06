namespace Content.Shared._RedStar.Beekeeping.Data;

/// <summary>
/// A tray a hive remembers. Bolted or not, wherever it is moved on the hive's grid, it stays the same tray.
/// </summary>
public sealed class BeehiveSource
{
    [ViewVariables]
    public EntityUid Tray;

    /// <summary>
    /// When the place was last found by a scout or visited with a plant on it.
    /// </summary>
    [ViewVariables]
    public TimeSpan LastSeen;

    /// <summary>
    /// False while the last check found no plant, or the tray was out of range, out of sight or carried away.
    /// Inactive places are checked again later and are the first to be forgotten.
    /// </summary>
    [ViewVariables]
    public bool Active = true;

    /// <summary>
    /// The place takes no visit before this time.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextVisit;

    /// <summary>
    /// A creature source: bites taken in the current window, and when that window started.
    /// </summary>
    [ViewVariables]
    public int Bites;

    [ViewVariables]
    public TimeSpan BiteWindowStart;

    public BeehiveSource(EntityUid tray, TimeSpan lastSeen)
    {
        Tray = tray;
        LastSeen = lastSeen;
        NextVisit = lastSeen;
    }
}
