using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology;

[Serializable, NetSerializable]
public sealed partial class SampleCollectorDoAfter : SimpleDoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return new SampleCollectorDoAfter();
    }
}
