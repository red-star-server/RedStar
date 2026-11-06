using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Visuals;

/// <summary>
/// Sprite layers of a honeycomb that take the comb's color. The shine layer stays as it is.
/// </summary>
[Serializable, NetSerializable]
public enum BeeCombVisualLayers : byte
{
    Drips,
    Cells,
    Outline,
}
