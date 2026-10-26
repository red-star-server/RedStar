using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Body;

/// <summary>
/// Lets you install an organ by pressing Z, if you don't have one already installed.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(InstallableOrganComponent))]
public sealed partial class InstallableOrganComponent : Component
{
    /// <summary>
    /// Time required to install the organ into the user.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(4);
}
