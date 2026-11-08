using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Visuals;

/// <summary>
/// How full a frame is. Also used for the per-frame honey overlay on the hive.
/// </summary>
[Serializable, NetSerializable]
public enum BeeFrameFill : byte
{
    /// <summary>
    /// Empty foundation. The hive overlay is hidden.
    /// </summary>
    None,

    /// <summary>
    /// Some combs, not full yet. The hive overlay shows honey.
    /// </summary>
    Honey,

    /// <summary>
    /// Full and capped. The hive overlay shows wax.
    /// </summary>
    Wax,
}
