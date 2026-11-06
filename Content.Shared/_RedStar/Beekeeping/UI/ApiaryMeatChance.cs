using Content.Shared._RedStar.Beekeeping.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.UI;

[Serializable, NetSerializable]
public sealed class ApiaryMeatChance
{
    public ProtoId<BeeMeatPrototype> Meat;
    public float Percent;
}
