using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Visuals;

/// <summary>
/// Bool: a wild hive is at least half full of combs and shows its honey.
/// </summary>
[Serializable, NetSerializable]
public enum WildBeehiveVisuals : byte
{
    Honey,
}
