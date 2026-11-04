using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery;

/// <summary>
///     Prevents the entity from causing toxin damage to entities it does surgery on.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SanitizedComponent : Component
{
    /// <summary>
    /// Whether holding this item protects the surgeon from contaminating a wound.
    /// </summary>
    [DataField]
    public bool WorksInHands;
}
