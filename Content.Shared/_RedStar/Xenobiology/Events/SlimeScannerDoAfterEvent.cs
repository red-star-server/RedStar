using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.Events;

[Serializable, NetSerializable]
public sealed partial class SlimeScannerDoAfterEvent : SimpleDoAfterEvent;
