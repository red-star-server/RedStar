using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.Visuals;

/// <summary>
/// Controls the injector door sprite: closed when occupied, open when empty.
/// </summary>
[Serializable, NetSerializable]
public enum MutagenicInjectorVisuals
{
    DoorState,
    DoorLayer
}
