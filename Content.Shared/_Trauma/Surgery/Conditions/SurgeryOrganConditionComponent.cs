using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Conditions;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryOrganConditionComponent : Component
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    [DataField]
    public bool Inverse;

    [DataField]
    public bool Reattaching;
}
