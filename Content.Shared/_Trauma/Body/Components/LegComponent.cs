using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Body;

[RegisterComponent, NetworkedComponent]
public sealed partial class LegComponent : Component
{
    [DataField]
    public float WalkSpeed = MovementSpeedModifierComponent.DefaultBaseWalkSpeed;

    [DataField]
    public float SprintSpeed = MovementSpeedModifierComponent.DefaultBaseSprintSpeed;

    [DataField]
    public float Acceleration = MovementSpeedModifierComponent.DefaultAcceleration;
}
