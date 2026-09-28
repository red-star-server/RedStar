using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.Events;

[Serializable, NetSerializable]
public sealed partial class XenobiologyAnalysisDoAfterEvent : DoAfterEvent
{
    [DataField]
    public int Generation;

    public override DoAfterEvent Clone() => this;
}
