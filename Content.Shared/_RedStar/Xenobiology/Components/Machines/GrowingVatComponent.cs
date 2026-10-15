namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

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
