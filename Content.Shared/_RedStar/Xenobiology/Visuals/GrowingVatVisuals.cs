using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.Visuals;

/// <summary>
/// Identifies the cultivation and solution layers of the vat sprite.
/// </summary>
[Serializable, NetSerializable]
public enum GrowingVatVisuals : byte
{
    Processing,
    GlowLayer,
    BubblesLayer
}
