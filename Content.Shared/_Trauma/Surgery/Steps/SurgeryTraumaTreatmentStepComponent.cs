using Robust.Shared.GameStates;

using Content.Shared._Trauma.Common.Traumas;
using Content.Shared.FixedPoint;

namespace Content.Shared._Trauma.Surgery.Steps;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryTraumaTreatmentStepComponent : Component
{
    [DataField]
    public TraumaType TraumaType = TraumaType.BoneDamage;

    [DataField]
    public FixedPoint2 Amount = 5;
}
