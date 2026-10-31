using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Data;

[Serializable, NetSerializable]
public enum ApiaryTrayState : byte
{
    /// <summary>The plant is ripe: the workers visit it.</summary>
    Ripe,
    /// <summary>The plant is still growing.</summary>
    Growing,
    /// <summary>No plant, or the workers cannot reach the tray now.</summary>
    Blind,
}
