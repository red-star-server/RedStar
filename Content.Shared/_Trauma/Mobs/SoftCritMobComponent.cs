using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Mobs;

/// <summary>
/// Marker component added to post-init mobs that are currently in softcrit.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SoftCritMobComponent : Component;
