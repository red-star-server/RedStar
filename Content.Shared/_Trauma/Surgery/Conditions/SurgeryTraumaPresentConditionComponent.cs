using Content.Shared._Trauma.Common.Traumas;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Conditions;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryTraumaPresentConditionComponent : Component
{
    [DataField("trauma")]
    public TraumaType TraumaType = TraumaType.BoneDamage;

    [DataField]
    public bool Inverted;
}
