using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Visuals;

/// <summary>
/// What a queen or a bee shows of its colony's traits: the colour of its grey mask (the payload, or honey) and the
/// angry eyes of aggressive bees.
/// </summary>
[Serializable, NetSerializable]
public enum BeeTraitVisuals : byte
{
    GreyColor,
    Angry,
}
