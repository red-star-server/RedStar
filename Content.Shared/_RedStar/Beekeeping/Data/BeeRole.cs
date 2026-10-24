using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Data;

[Serializable, NetSerializable]
public enum BeeRole : byte
{
    Scout,
    Pollinator,
}
