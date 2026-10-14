using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology;

[Serializable, NetSerializable]
public sealed partial class CellCollectorDoAfter : SimpleDoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return new CellCollectorDoAfter();
    }
}
