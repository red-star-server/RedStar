namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent]
public sealed partial class GeneticSequencerComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [DataField]
    public string VialSlot = "vialSlot";

    /// <summary>
    /// Stability removed from the selected sample after an incorrect sequencing attempt.
    /// </summary>
    [DataField]
    public float IncorrectStabilityCost = 0.03f;
}
