using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Conditions;

/// <summary>
/// Requires that an organ slot does (not) exist on the target part for a surgery to be possible.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryOrganSlotConditionComponent : Component
{
    /// <summary>
    /// Organ category whose slot is required on the selected part.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> OrganSlot;

    /// <summary>
    /// Whether the organ slot must be absent rather than present.
    /// </summary>
    [DataField]
    public bool Inverse;
}
