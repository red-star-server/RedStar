using Robust.Shared.GameStates;

namespace Content.Shared._DV;

/// <summary>
/// Stores the carrier of an entity being carried.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CarryingSystem))]
[AutoGenerateComponentState]
public sealed partial class BeingCarriedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Carrier;

    /// <summary>
    /// Whether the entity was standing before it was carried.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool WasStanding;

    /// <summary>
    /// Prevents reentrant cleanup and disables carry restrictions during release.
    /// </summary>
    [AutoNetworkedField]
    public bool Releasing;
}
