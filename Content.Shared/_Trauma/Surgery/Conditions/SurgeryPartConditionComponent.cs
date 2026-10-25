using Robust.Shared.GameStates;

using Content.Shared._Trauma.Common.Body;

namespace Content.Shared._Trauma.Surgery.Conditions;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryPartConditionComponent : Component
{
    [DataField]
    public HashSet<BodyPartType> Parts;

    [DataField]
    public BodyPartSymmetry? Symmetry;

    [DataField]
    public bool Inverse;
}
