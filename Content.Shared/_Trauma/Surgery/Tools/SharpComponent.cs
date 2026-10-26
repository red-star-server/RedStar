using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Tools;

/// <summary>
/// Grants fallback scalpel and bone-saw qualities to sharp items.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SharpComponent : Component
{
    /// <summary>
    /// Whether this item had <c>SurgeryToolComponent</c> before sharp was added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HadSurgeryTool;

    /// <summary>
    /// Whether this item had <c>ScalpelComponent</c> before sharp was added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HadScalpel;

    /// <summary>
    /// Whether this item had <c>BoneSawComponent</c> before sharp was added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HadBoneSaw;
}
