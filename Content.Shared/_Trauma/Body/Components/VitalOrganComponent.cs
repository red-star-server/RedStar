using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Body;

/// <summary>
/// Marks an organ as vital: removing it from a living body triggers <see cref="DelayedDeath.DelayedDeathComponent"/>.
/// Reinserting it while the body still has a brain removes the component again.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VitalOrganComponent : Component;
