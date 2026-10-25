using Robust.Shared.GameStates;

using Content.Shared.Whitelist;

namespace Content.Shared._Trauma.Surgery.Conditions;

/// <summary>
/// Checks the target's body against a whitelist and/or blacklist for the surgery to be valid.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryBodyComponentConditionComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
