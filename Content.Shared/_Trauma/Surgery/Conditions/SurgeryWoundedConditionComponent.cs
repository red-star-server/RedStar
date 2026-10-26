using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Conditions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryWoundedConditionComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> DamageGroup = "Brute";
}
