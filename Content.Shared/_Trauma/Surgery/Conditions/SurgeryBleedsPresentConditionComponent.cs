using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Conditions;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryBleedsPresentConditionComponent : Component
{
    /// <summary>
    /// Whether the condition requires bleeding to be absent instead.
    /// </summary>
    [DataField]
    public bool Inverted;
}
