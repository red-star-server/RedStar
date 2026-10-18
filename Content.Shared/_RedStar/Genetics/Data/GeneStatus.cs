namespace Content.Shared._RedStar.Genetics.Data;

/// <summary>
/// Expression state, independent of whether a gene is inherited or acquired.
/// </summary>
public enum GeneStatus : byte
{
    /// <summary>
    /// Present genetic material that has not been expressed.
    /// </summary>
    Raw = 0,

    /// <summary>
    /// Present genetic material whose traits are expressed.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Present genetic material with expression disabled; bulk activation leaves it suppressed.
    /// </summary>
    Suppressed = 2
}
