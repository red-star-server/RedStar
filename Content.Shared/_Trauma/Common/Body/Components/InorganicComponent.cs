using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Common.Body;

/// <summary>
/// An inorganic organ/bodypart.
/// Used for some damage splitting types e.g. in reagents.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InorganicComponent : Component;
