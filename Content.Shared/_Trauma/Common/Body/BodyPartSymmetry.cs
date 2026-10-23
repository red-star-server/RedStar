using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Common.Body;

/// <summary>
/// Defines the symmetry of a body part.
/// </summary>
[Serializable, NetSerializable]
public enum BodyPartSymmetry : byte
{
    None = 0,
    Left,
    Right
}
