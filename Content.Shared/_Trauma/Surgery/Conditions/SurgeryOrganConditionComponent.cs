using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Conditions;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryOrganConditionComponent : Component
{
    /// <summary>
    /// Organ category checked by the operation.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    /// <summary>
    /// Whether the organ must be absent rather than present.
    /// </summary>
    [DataField]
    public bool Inverse;

    /// <summary>
    /// Whether the operation checks the organ's reattachment state.
    /// </summary>
    [DataField]
    public bool Reattaching;
}
