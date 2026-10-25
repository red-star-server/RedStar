using Robust.Shared.GameStates;

using Content.Shared.Humanoid;

namespace Content.Shared._Trauma.Surgery.Conditions;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryMarkingConditionComponent : Component
{
    [DataField]
    public bool Inverse;

    /// <summary>
    ///     The marking category to check for.
    /// </summary>
    [DataField]
    public HumanoidVisualLayers MarkingCategory;

    /// <summary>
    ///     Can be either a segment of a marking ID, or an entire ID that will be checked
    ///     against the entity to validate that the marking is not already present.
    /// </summary>
    [DataField]
    public String MatchString = "";
}
