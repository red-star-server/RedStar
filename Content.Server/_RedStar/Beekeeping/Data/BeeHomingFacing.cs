namespace Content.Server._RedStar.Beekeeping.Data;

public enum BeeHomingFacing : byte
{
    /// <summary>
    /// Not turned at all: an upright sprite that looks fine in any direction.
    /// </summary>
    None,

    /// <summary>
    /// Side view facing left: mirrored while flying to the right.
    /// </summary>
    Mirror,

    /// <summary>
    /// Top-down view with the head up: rotated to point at the hive.
    /// </summary>
    Rotate,
}
