namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent]
public sealed partial class CellSequencerComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [DataField]
    public string VialSlot = "vialSlot";

    public int FeedbackSample = -1;
    public string FeedbackFamily = string.Empty;
    public int[] FeedbackOrder = [];
}
