using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Visuals;

/// <summary>
/// Size of a flying bee sprite. The client animates the change between full and hidden.
/// </summary>
[Serializable, NetSerializable]
public enum BeeFlightSize : byte
{
    Full,
    Shrinking,
    Hidden,
    Growing,
}
