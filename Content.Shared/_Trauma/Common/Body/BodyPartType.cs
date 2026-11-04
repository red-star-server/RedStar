using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Common.Body;

/// <summary>
/// Defines the type of a body part.
/// </summary>
[Serializable, NetSerializable]
public enum BodyPartType : byte
{
    Other = 0,
    Torso = 1 << 0,
    Head = 1 << 1,
    Arm = 1 << 2,
    Hand = 1 << 3,
    Leg = 1 << 4,
    Foot = 1 << 5,
    Tail = 1 << 6,
    Wings = 1 << 7
}
