using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Data;

[Serializable, NetSerializable]
public enum ApiaryNectarUse : byte
{
    /// <summary>Nothing to spend it on.</summary>
    Idle,
    /// <summary>The colony is not full yet but no comb can be built: nectar waits for the next bee.</summary>
    Bees,
    /// <summary>The colony grows and the rest of the nectar becomes combs.</summary>
    BeesAndCombs,
    /// <summary>The colony is full: nectar becomes combs.</summary>
    Combs,
    /// <summary>Every frame is full: nectar piles up until combs are cut.</summary>
    FramesFull,
    /// <summary>No workers at home to build combs.</summary>
    NoWorkers,
    /// <summary>A hive without frames: no room for bees or combs.</summary>
    NoFrames
}
