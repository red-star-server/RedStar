namespace Content.Server._RedStar.Beekeeping.Data;

public enum BeeErrandAct : byte
{
    None,

    /// <summary>
    /// Into the flower, a while inside, and out again.
    /// </summary>
    Visit,

    /// <summary>
    /// Into the hive: the errand is over.
    /// </summary>
    Enter
}
