using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Events;

/// <summary>
/// Collecting royal jelly from the queen egg into the container in hand.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BeeJellyDoAfterEvent : SimpleDoAfterEvent;
