using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Data;

[Serializable, NetSerializable]
public enum ApiaryScanKind : byte
{
    Hive,
    Queen,
    Scout,
    Pollinator
}
