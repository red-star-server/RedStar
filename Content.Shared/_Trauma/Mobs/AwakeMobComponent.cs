using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Mobs;

/// <summary>
/// Marker component added to post-init mobs that are both alive and awake.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AwakeMobComponent : Component;
