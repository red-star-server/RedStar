namespace Content.Shared._Sirena.Humanoid;

/// <summary>
/// Character preference regarding participation in ERP.
/// </summary>
public enum ErpStatus : byte
{
    /// <summary>
    /// The player does not consent to ERP involving this character.
    /// </summary>
    No = 0,

    /// <summary>
    /// The player is open to ERP with limitations.
    /// </summary>
    Partial = 1,

    /// <summary>
    /// The player is generally open to ERP involving this character.
    /// </summary>
    Full = 2
}
