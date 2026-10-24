using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Visuals;

/// <summary>
/// Appearance data keys of the beehive.
/// </summary>
[Serializable, NetSerializable]
public enum BeehiveVisuals : byte
{
    /// <summary>
    /// Int: how many frames are inserted. Picks the <c>frames-N</c> base state.
    /// </summary>
    Frames,

    /// <summary>
    /// <see cref="BeeFrameFill"/> of the first inserted frame (counting only inserted frames, not slots).
    /// </summary>
    Frame1,

    /// <summary>
    /// <see cref="BeeFrameFill"/> of the second inserted frame.
    /// </summary>
    Frame2,

    /// <summary>
    /// <see cref="BeeFrameFill"/> of the third inserted frame.
    /// </summary>
    Frame3,

    /// <summary>
    /// <see cref="BeeFrameFill"/> of the fourth inserted frame.
    /// </summary>
    Frame4,
}
