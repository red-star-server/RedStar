using Content.Shared._RedStar.Beekeeping.Data;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.UI;

[Serializable, NetSerializable]
public sealed class ApiaryTrayData
{
    public NetEntity Tray;
    public NetEntity? Plant;
    public EntProtoId? PlantPrototype;
    public ApiaryTrayState State;
}
