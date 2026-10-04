namespace Content.Shared._DV;

/// <summary>
/// Entities with this component override the number of free hands required to carry an entity,
/// always requiring one hand instead.
/// </summary>
[RegisterComponent, Access(typeof(CarryingSystem))]
public sealed partial class CarrierOneHandComponent : Component;
