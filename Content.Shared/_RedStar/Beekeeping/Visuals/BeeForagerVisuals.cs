using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Visuals;

/// <summary>
/// Appearance data keys of the visual forager bee.
/// </summary>
[Serializable, NetSerializable]
public enum BeeForagerVisuals : byte
{
    /// <summary>
    /// Bool: whether the bee flies to the right, so the side-view sprite is mirrored. Also used by bee and queen
    /// items flying home.
    /// </summary>
    FacingRight,

    /// <summary>
    /// <see cref="BeeFlightSize"/>: the bee dives into a flower or the hive by shrinking, and comes back out.
    /// </summary>
    Size,

    /// <summary>
    /// Bool: a bee or queen item is flying home on its own right now, so it wobbles like a flying bee.
    /// </summary>
    Flying,
}
