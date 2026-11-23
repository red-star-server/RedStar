namespace Content.Server._RedStar.Xenobiology.Components;

/// <summary>
/// Marks a growing vat that awaits evaluation or is processing a recipe so that only such vats are updated.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveGrowingVatComponent : Component;
