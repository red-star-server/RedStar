using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery;

/// <summary>
/// Component for surgery entity prototypes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[EntityCategory("Surgeries")]
public sealed partial class SurgeryComponent : Component
{
    /// <summary>
    /// Priority used to order available operations in the surgery UI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Priority;

    /// <summary>
    /// Operation that must be completed before this one can proceed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? Requirement;

    /// <summary>
    /// Ordered step prototypes that make up this operation.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<EntProtoId> Steps = new();
}
