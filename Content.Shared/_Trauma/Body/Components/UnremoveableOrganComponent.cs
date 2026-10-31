using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Body;

/// <summary>
/// Prevents removing this organ from a body and logs an error if it somehow happens.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UnremoveableOrganComponent : Component
{
}
