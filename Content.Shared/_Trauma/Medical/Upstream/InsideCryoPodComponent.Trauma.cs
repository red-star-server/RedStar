namespace Content.Shared.Medical.Cryogenics;

/// <summary>
/// Trauma - Stores sleep action added by <c>CryoPodSleepingSystem</c>
/// </summary>
public sealed partial class InsideCryoPodComponent
{
    /// <summary>
    /// Action granted to the occupant to sleep inside the cryopod.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SleepAction;
}
