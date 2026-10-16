namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent]
public sealed partial class CellSequencerComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [DataField]
    public string VialSlot = "vialSlot";

    [DataField]
    public float IncorrectStabilityCost = 0.1f;
}
