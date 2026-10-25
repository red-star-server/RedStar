using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

using Content.Shared.Body;

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
