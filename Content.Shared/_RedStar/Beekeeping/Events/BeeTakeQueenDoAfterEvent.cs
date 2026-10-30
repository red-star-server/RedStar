using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Events;

/// <summary>
/// Taking the queen out of a hive. Raised on the hive.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BeeTakeQueenDoAfterEvent : SimpleDoAfterEvent;
