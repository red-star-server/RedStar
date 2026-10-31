using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgerySpeedModifierComponent : Component
{
    /// <summary>
    /// Multiplier applied to this surgeon's operating speed.
    /// </summary>
    [DataField]
    public float SpeedModifier = 1.5f;
}
