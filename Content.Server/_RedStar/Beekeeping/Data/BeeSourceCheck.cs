namespace Content.Server._RedStar.Beekeeping.Data;

public enum BeeSourceCheck : byte
{
    /// <summary>
    /// The tray is gone. The record is deleted.
    /// </summary>
    Invalid,

    /// <summary>
    /// No living plant in the tray, the tray is too far, out of sight, inside something or on another grid.
    /// </summary>
    Inactive,

    /// <summary>
    /// A living plant that does not give nectar yet.
    /// </summary>
    Unripe,

    /// <summary>
    /// A ripe plant: a visit brings nectar.
    /// </summary>
    Ripe,
}
