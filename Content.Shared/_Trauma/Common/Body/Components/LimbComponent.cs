using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Common.Body;

/// <summary>
/// A limb that can easily be severed.
/// Arms/legs/tail
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LimbComponent : Component;
