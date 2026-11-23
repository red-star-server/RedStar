namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent]
public sealed partial class GeneticSequencerComponent : Component
{
    /// <summary>
    /// Item slot holding the Petri dish.
    /// </summary>
    [DataField]
    public string DishSlot = "dishSlot";

    /// <summary>
    /// Item slot holding the vial that receives extracted genes.
    /// </summary>
    [DataField]
    public string VialSlot = "vialSlot";

    /// <summary>
    /// Stability removed from the selected sample after an incorrect sequencing attempt.
    /// </summary>
    [DataField]
    public float IncorrectStabilityCost = 0.03f;
}
