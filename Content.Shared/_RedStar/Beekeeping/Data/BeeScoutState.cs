using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Data;

/// <summary>
/// Where the only scout of a hive is.
/// </summary>
[Serializable, NetSerializable]
public enum BeeScoutState : byte
{
    /// <summary>
    /// At home, counted in <see cref="BeehiveComponent.Bees"/>, scouting on its own.
    /// </summary>
    Home,

    /// <summary>
    /// Outside as an item, counted in <see cref="BeehiveComponent.OutBees"/>. Nothing new is found meanwhile.
    /// </summary>
    Outside,

    /// <summary>
    /// Dead; a worker takes its place at <see cref="BeehiveComponent.ScoutReplaceAt"/>.
    /// </summary>
    Lost,
}
