using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Events;

[Serializable, NetSerializable]
public sealed partial class ApiaryAnalyzerDoAfterEvent : SimpleDoAfterEvent;
