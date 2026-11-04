using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Steps.Parts;

/// <summary>
/// Added to reattached bodyparts or internal organs just before the surgery is finished.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OrganReattachedComponent : Component;
