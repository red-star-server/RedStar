using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology;

[Serializable, NetSerializable]
public enum SlimeTemperament : byte
{
    Calm,
    Restless,
    Aggressive
}

[Serializable, NetSerializable]
public enum SlimeCrowding : byte
{
    Low,
    Crowded,
    Severe
}
