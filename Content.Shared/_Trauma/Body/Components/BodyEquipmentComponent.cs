using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Body;

/// <summary>
/// Prevents equipping items into inventory slots that are missing body parts.
/// I.e. you can't wear a hat without a head.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BodyEquipmentComponent : Component;
