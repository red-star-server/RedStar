using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Data;

/// <summary>
/// Overcrowding state of a hive.
/// </summary>
[Serializable, NetSerializable]
public enum BeehiveCrowding : byte
{
    None,

    /// <summary>
    /// Over the limit, within the grace period. Bees may still be returned.
    /// </summary>
    Cramped,

    /// <summary>
    /// Over the limit after the grace period. Extra bees leave and returns are refused.
    /// </summary>
    Leaving,
}
