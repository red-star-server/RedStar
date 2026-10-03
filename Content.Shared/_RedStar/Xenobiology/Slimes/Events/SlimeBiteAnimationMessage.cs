using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.Slimes.Events;

[Serializable, NetSerializable]
public sealed class SlimeBiteAnimationMessage : EntityEventArgs
{
    public NetEntity Entity;
    public Angle Angle;
}
