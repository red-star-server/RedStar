using Content.Shared._Trauma.Common.Wounds;
using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Body;

/// <summary>
/// Shows the mob's limb statuses as an alert on screen.
/// It can be clicked to show detailed info in chat.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class BodyStatusComponent : Component
{
    /// <summary>
    /// What is the current integrity of each body part?
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, WoundableSeverity> BodyStatus = new();
}
