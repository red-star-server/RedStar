using Robust.Shared.GameStates;


namespace Content.Shared._Trauma.Surgery.Steps.Parts;

/// <summary>
/// Marker component added to limbs during slot creation surgery.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class JointStitchedComponent : Component;
