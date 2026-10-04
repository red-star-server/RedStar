using Robust.Shared.GameStates;

namespace Content.Shared._DV;

[RegisterComponent, NetworkedComponent, Access(typeof(CarryingSystem))]
public sealed partial class CarriableComponent : Component
{
    /// <summary>
    /// Number of free hands required to carry the entity.
    /// </summary>
    [DataField]
    public int FreeHandsRequired = 2;

    /// <summary>
    /// Base pickup duration before adjusting for the mass ratio.
    /// </summary>
    [DataField]
    public TimeSpan PickupDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Maximum pickup duration allowed before the entity is considered too heavy.
    /// </summary>
    [DataField]
    public TimeSpan MaximumPickupDuration = TimeSpan.FromSeconds(9);

    /// <summary>
    /// Pickup duration multiplier when the entity is not knocked down.
    /// </summary>
    [DataField]
    public float StandingPickupMultiplier = 2f;

    /// <summary>
    /// Base throw speed before adjusting for the mass ratio.
    /// </summary>
    [DataField]
    public float ThrowSpeed = 5f;

    /// <summary>
    /// Speed penalty divided by the squared carrier-to-carried mass ratio.
    /// </summary>
    [DataField]
    public float SpeedPenalty = 0.15f;

    /// <summary>
    /// Minimum movement speed multiplier while carrying this entity.
    /// </summary>
    [DataField]
    public float MinimumSpeedModifier = 0.1f;
}
