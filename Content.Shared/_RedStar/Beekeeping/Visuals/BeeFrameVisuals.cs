using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Visuals;

/// <summary>
/// Appearance data keys of a bee frame.
/// </summary>
[Serializable, NetSerializable]
public enum BeeFrameVisuals : byte
{
    /// <summary>
    /// <see cref="BeeFrameFill"/> of the frame.
    /// </summary>
    Fill,

    /// <summary>
    /// Color: what the combs on the frame look like, from the reagents in them.
    /// </summary>
    Color,
}
