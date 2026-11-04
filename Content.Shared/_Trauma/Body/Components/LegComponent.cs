using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Body;

[RegisterComponent, NetworkedComponent]
public sealed partial class LegComponent : Component
{
    /// <summary>
    /// Walking speed contributed by this leg before the body's leg-count scaling.
    /// </summary>
    [DataField]
    public float WalkSpeed = MovementSpeedModifierComponent.DefaultBaseWalkSpeed;

    /// <summary>
    /// Sprinting speed contributed by this leg before the body's leg-count scaling.
    /// </summary>
    [DataField]
    public float SprintSpeed = MovementSpeedModifierComponent.DefaultBaseSprintSpeed;

    /// <summary>
    /// Acceleration contributed by this leg before the body's leg-count scaling.
    /// </summary>
    [DataField]
    public float Acceleration = MovementSpeedModifierComponent.DefaultAcceleration;
}
