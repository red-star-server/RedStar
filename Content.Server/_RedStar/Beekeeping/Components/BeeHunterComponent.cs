namespace Content.Server._RedStar.Beekeeping.Components;

/// <summary>
/// A meat colony's bee on an errand: it goes for a creature and bites it (<see cref="MeatBeehiveSystem"/>), and whoever
/// hits it is remembered by the colony.
/// </summary>
[RegisterComponent]
public sealed partial class BeeHunterComponent : Component;
