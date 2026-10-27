using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Conditions;

/// <summary>
/// Checks the target's body against a whitelist and/or blacklist for the surgery to be valid.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryBodyComponentConditionComponent : Component
{
    /// <summary>
    /// Body requirements that must match for the operation to be available.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Body properties that prevent the operation from being available.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
