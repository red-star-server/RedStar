namespace Content.Shared.Medical.Cryogenics;

/// <summary>
/// Trauma - Stores sleep action added by <c>CryoPodSleepingSystem</c>
/// </summary>
public sealed partial class InsideCryoPodComponent
{
    [DataField, AutoNetworkedField]
    public EntityUid? SleepAction;
}
