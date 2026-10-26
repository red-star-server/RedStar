using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryStepCavityEffectComponent : Component
{
    /// <summary>
    /// Whether the step inserts an item into the cavity or removes it.
    /// </summary>
    [DataField]
    public string Action = "Insert";
}
