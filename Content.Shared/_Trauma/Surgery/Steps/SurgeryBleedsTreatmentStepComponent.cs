using Robust.Shared.GameStates;

using Content.Shared.FixedPoint;

namespace Content.Shared._Trauma.Surgery.Steps;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryBleedsTreatmentStepComponent : Component
{
    [DataField]
    public FixedPoint2 Amount = 5;
}
