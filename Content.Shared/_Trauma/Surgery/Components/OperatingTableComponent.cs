using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery;

[RegisterComponent, NetworkedComponent]
public sealed partial class OperatingTableComponent : Component
{
    /// <summary>
    /// Surgery speed multiplier provided while the patient lies on this table.
    /// </summary>
    [DataField]
    public float SpeedModifier = 1f;
}
