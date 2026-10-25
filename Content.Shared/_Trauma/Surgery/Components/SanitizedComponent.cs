using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery;

/// <summary>
///     Prevents the entity from causing toxin damage to entities it does surgery on.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SanitizedComponent : Component
{
    [DataField]
    public bool WorksInHands;
}
