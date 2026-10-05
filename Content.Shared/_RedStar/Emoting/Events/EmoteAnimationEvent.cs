using Content.Shared._RedStar.Emoting.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Emoting.Events;

/// <summary>
/// Sent to clients in PVS when an entity performs a visual emote animation.
/// </summary>
[Serializable, NetSerializable]
public sealed class EmoteAnimationEvent(
    NetEntity entity,
    ProtoId<EmoteAnimationPrototype> animation) : EntityEventArgs
{
    public readonly NetEntity Entity = entity;
    public readonly ProtoId<EmoteAnimationPrototype> Animation = animation;
}
