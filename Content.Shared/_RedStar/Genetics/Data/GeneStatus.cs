namespace Content.Shared._RedStar.Genetics.Data;

/// <summary>
/// Expression state, independent of whether a gene is inherited or acquired.
/// </summary>
public enum GeneStatus : byte
{
    /// <summary>
    /// Present genetic material that is not expressed. It still reserves load.
    /// </summary>
    Dormant = 0,

    /// <summary>
    /// Present genetic material whose traits are expressed.
    /// </summary>
    Active = 1
}
