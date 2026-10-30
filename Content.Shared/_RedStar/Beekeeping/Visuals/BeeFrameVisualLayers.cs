using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Visuals;

/// <summary>
/// Sprite layers of a bee frame.
/// </summary>
[Serializable, NetSerializable]
public enum BeeFrameVisualLayers : byte
{
    Base,

    /// <summary>
    /// Grey overlay tinted with the color of the combs.
    /// </summary>
    Comb,

    /// <summary>
    /// The same shape in wax, shown on a capped frame instead of the combs. Not tinted.
    /// </summary>
    Caps,

    /// <summary>
    /// Grey comb outline tinted with the color of the combs.
    /// </summary>
    Outline,
}
