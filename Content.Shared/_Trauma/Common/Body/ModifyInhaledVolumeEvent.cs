namespace Content.Shared._Trauma.Common.Body;

/// <summary>
/// Raised on a mob to modify how much of the breathed atmosphere it can inhale.
/// </summary>
[ByRefEvent]
public record struct ModifyInhaledVolumeEvent(float Volume);
