using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Data;

/// <summary>
/// Coarse nectar stock of a hive.
/// </summary>
[Serializable, NetSerializable]
public enum BeehiveNectarLevel : byte
{
    Empty,
    Low,
    Medium,
    High,
}
