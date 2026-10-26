using Content.Shared._Trauma.Common.Body;
using Robust.Shared.GameStates;

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
