using Content.Shared._RedStar.Emoting.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Emoting.Events;

/// <summary>
/// Starts two synchronized cosmetic emote animations.
/// </summary>
[Serializable, NetSerializable]
public sealed class PairedEmoteAnimationEvent(
    NetEntity initiator,
    NetEntity target,
    ProtoId<EmoteAnimationPrototype> initiatorAnimation,
    ProtoId<EmoteAnimationPrototype> targetAnimation,
    float approachOffset) : EntityEventArgs
{
    public readonly float ApproachOffset = approachOffset;

    public readonly NetEntity Initiator = initiator;
    public readonly NetEntity Target = target;

    public readonly ProtoId<EmoteAnimationPrototype> InitiatorAnimation = initiatorAnimation;
    public readonly ProtoId<EmoteAnimationPrototype> TargetAnimation = targetAnimation;
}
