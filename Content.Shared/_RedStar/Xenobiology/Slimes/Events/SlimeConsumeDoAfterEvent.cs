using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.Slimes.Events;

[Serializable, NetSerializable]
public sealed partial class SlimeConsumeDoAfterEvent : SimpleDoAfterEvent;
