namespace Content.Server._RedStar.Beekeeping.Data;

public enum BeeErrandArrival : byte
{
    /// <summary>
    /// Into the flower for a while, then home.
    /// </summary>
    Visit,

    /// <summary>
    /// Straight home.
    /// </summary>
    TurnBack,

    /// <summary>
    /// On to a new target or spot the handler set; the errand goes on.
    /// </summary>
    KeepGoing,

    /// <summary>
    /// The errand is over and the bee is gone (a swarm that built its hive, bees that scattered).
    /// </summary>
    Gone,
}
