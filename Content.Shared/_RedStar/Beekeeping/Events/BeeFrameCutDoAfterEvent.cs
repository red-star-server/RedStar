using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Events;

/// <summary>
/// Cutting the combs off a frame with a sharp tool. Raised on the frame.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BeeFrameCutDoAfterEvent : SimpleDoAfterEvent;
