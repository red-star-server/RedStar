using Robust.Shared.Serialization;
using Content.Shared._Trauma.Common.Targeting;

namespace Content.Shared._Trauma.Targeting;

/// <summary>
/// Message sent by the client to change its mob's targeted part.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangeTargetMessage(TargetBodyPart part): EntityEventArgs
{
    public readonly TargetBodyPart BodyPart = part;
}
