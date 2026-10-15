namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

/// <summary>
/// Holds the cultivation timer. Genes, chemical requirements and mutation odds
/// belong to GeneticMutationPrototype, not to the machine.
/// </summary>
[RegisterComponent]
public sealed partial class GrowingVatComponent : Component
{
    [DataField]
    public string Solution = "vat";

    [ViewVariables]
    public EntityUid? Subject;

    [ViewVariables]
    public string? Recipe;

    [ViewVariables]
    public TimeSpan Progress;

    [ViewVariables]
    public float TickAccumulator;

    [ViewVariables]
    public bool Processing;
}
