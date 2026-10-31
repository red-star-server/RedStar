using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Visuals;

/// <summary>
/// Sprite layers of a flying bee (visual forager, bee item, queen item) that are mirrored when it faces right.
/// </summary>
[Serializable, NetSerializable]
public enum BeeForagerVisualLayers : byte
{
    Body,
    Grey,
    Wings,
    Angry,
}
