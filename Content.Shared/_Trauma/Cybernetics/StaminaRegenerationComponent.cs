using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Cybernetics;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StaminaRegenerationComponent : Component
{
    /// <summary>
    /// How much stamina is regenerated per second.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RegenerationRate = -1.0f;
}
