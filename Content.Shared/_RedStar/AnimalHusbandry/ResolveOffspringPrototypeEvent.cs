namespace Content.Shared.Nutrition.AnimalHusbandry;

/// <summary>
/// Allows the carrier to choose the prototype of an individual offspring after
/// the reproductive system has determined how many offspring to spawn.
/// </summary>
[ByRefEvent]
public record struct ResolveOffspringPrototypeEvent(string Prototype);
