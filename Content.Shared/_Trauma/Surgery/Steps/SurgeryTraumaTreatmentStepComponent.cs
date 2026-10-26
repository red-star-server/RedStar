using Content.Shared._Trauma.Common.Traumas;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Steps;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryTraumaTreatmentStepComponent : Component
{
    /// <summary>
    /// Trauma type treated by this step.
    /// </summary>
    [DataField]
    public TraumaType TraumaType = TraumaType.BoneDamage;

    /// <summary>
    /// Severity removed from the selected trauma by each treatment step.
    /// </summary>
    [DataField]
    public FixedPoint2 Amount = 5;
}
