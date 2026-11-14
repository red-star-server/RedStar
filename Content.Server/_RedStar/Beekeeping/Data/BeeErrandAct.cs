namespace Content.Server._RedStar.Beekeeping.Data;

public enum BeeErrandAct : byte
{
    None,

    /// <summary>
    /// Into the flower, a while inside, and out again.
    /// </summary>
    Visit,

    /// <summary>
    /// Into a vent, through the pipes, and out of the vent at the other end.
    /// </summary>
    Pipe,

    /// <summary>
    /// Into the hive: the errand is over.
    /// </summary>
    Enter,
}
