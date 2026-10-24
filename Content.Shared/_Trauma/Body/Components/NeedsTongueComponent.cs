using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

using Content.Shared.Body;

namespace Content.Shared._Trauma.Body;

/// <summary>
/// Body component that requires there is a tongue organ installed and healthy to speak.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(NeedsTongueSystem))]
public sealed partial class NeedsTongueComponent : Component
{
    [DataField]
    public ProtoId<OrganCategoryPrototype> Category = "Tongue";
}
