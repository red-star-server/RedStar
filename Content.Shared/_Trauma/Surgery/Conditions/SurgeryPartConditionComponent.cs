using Content.Shared._Trauma.Common.Body;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Conditions;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryPartConditionComponent : Component
{
    /// <summary>
    /// Body-part types on which the operation may be performed.
    /// </summary>
    [DataField]
    public HashSet<BodyPartType> Parts;

    /// <summary>
    /// Optional side required for the selected body part.
    /// </summary>
    [DataField]
    public BodyPartSymmetry? Symmetry;

    /// <summary>
    /// Whether matching body parts are excluded rather than allowed.
    /// </summary>
    [DataField]
    public bool Inverse;
}
