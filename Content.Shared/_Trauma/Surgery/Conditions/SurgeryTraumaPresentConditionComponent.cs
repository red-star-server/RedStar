using Content.Shared._Trauma.Common.Traumas;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Conditions;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryTraumaPresentConditionComponent : Component
{
    /// <summary>
    /// Trauma type that must be present on the selected part.
    /// </summary>
    [DataField("trauma")]
    public TraumaType TraumaType = TraumaType.BoneDamage;

    /// <summary>
    /// Whether the specified trauma must be absent instead.
    /// </summary>
    [DataField]
    public bool Inverted;
}
