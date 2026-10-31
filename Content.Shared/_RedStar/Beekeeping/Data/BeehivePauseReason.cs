using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Data;

/// <summary>
/// Why the bees of a hive are not working.
/// </summary>
[Serializable, NetSerializable]
public enum BeehivePauseReason : byte
{
    None,
    Vacuum,
    HighPressure,
    Cold,
    Heat,
    Fire
}
