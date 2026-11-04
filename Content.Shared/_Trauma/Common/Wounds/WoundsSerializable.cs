using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Common.Wounds;

[Serializable, NetSerializable]
public enum WoundSeverity : byte
{
    Healed,
    Minor,
    Moderate,
    Severe,
    Critical,
    Loss
}

[Serializable, NetSerializable]
public enum BleedingSeverity : byte
{
    Minor,
    Severe
}

[Serializable, NetSerializable]
public enum WoundableSeverity : byte
{
    Healthy,
    Minor,
    Moderate,
    Severe,
    Critical,
    Mangled,
    Severed
}
