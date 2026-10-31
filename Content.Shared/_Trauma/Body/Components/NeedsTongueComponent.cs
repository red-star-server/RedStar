using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Body;

/// <summary>
/// Body component that requires there is a tongue organ installed and healthy to speak.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(NeedsTongueSystem))]
public sealed partial class NeedsTongueComponent : Component
{
    /// <summary>
    /// Organ category required for this entity to speak.
    /// </summary>
    [DataField]
    public ProtoId<OrganCategoryPrototype> Category = "Tongue";
}
