using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.UI;

[Serializable, NetSerializable]
public sealed class ApiaryFrameData
{
    public NetEntity Frame;
    public EntProtoId? Prototype;
    public int Combs;
    public int MaxCombs;
}
