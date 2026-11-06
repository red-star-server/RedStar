using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Data;

/// <summary>
/// What a colony feeds on.
/// </summary>
[Serializable, NetSerializable]
public enum BeehiveDiet : byte
{
    /// <summary>Nectar from plants.</summary>
    Plants,
    /// <summary>Blood from the creatures around.</summary>
    Meat,
}
