using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.Slimes;

[Serializable, NetSerializable]
public enum SlimeStage : byte
{
    Baby,
    Adult
}
