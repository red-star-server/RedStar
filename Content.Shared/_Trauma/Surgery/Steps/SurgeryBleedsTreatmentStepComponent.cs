using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Steps;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryBleedsTreatmentStepComponent : Component
{
    /// <summary>
    /// Bleeding scale removed from wounds by each treatment step.
    /// </summary>
    [DataField]
    public FixedPoint2 Amount = 5;
}
