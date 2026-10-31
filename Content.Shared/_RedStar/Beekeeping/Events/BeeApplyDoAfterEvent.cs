using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Events;

/// <summary>
/// Applying a bee in hand to a tray: a scout remembers it, a pollinator pollinates the plant. Raised on the bee.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BeeApplyDoAfterEvent : SimpleDoAfterEvent
{
}
