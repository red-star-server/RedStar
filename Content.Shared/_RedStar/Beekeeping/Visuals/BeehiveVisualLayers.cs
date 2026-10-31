using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Visuals;

/// <summary>
/// Sprite layers of the beehive, mapped in the prototype.
/// </summary>
[Serializable, NetSerializable]
public enum BeehiveVisualLayers : byte
{
    Base,
    Honey1,
    Honey2,
    Honey3,
    Honey4,
}
