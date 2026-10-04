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
}
