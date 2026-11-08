using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Visuals;

/// <summary>
/// Appearance data keys of a honeycomb.
/// </summary>
[Serializable, NetSerializable]
public enum BeeCombVisuals : byte
{
    /// <summary>
    /// Color: the mixed color of the reagents in the comb that are not honey.
    /// </summary>
    Color,

    /// <summary>
    /// Float, 0 to 1: how much of the comb is not honey. At 0 the layers keep their own honey colors.
    /// </summary>
    Share,
}
