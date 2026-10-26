using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Conditions;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryPartRemovedConditionComponent : Component
{
    /// <summary>
    /// Requires that the parent part can attach a new part to this slot.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;

    [DataField]
    public BodyPartType Part;

    [DataField]
    public BodyPartSymmetry? Symmetry;
}
