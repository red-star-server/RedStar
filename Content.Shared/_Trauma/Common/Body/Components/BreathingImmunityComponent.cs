namespace Content.Shared._Trauma.Common.Body;

/// <summary>
///     Disables a mobs need for air when this component is added.
///     It will neither breathe nor take airloss damage.
/// </summary>
[RegisterComponent]
public sealed partial class BreathingImmunityComponent : Component;
