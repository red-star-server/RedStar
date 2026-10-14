namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent]
public sealed partial class CellSequencerComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    public EntityUid? ActiveDish;
    public int ActiveRevision = -1;
    public HashSet<string> Decoded = [];
    public TimeSpan NextAttempt;
    public int FeedbackSample = -1;
    public int FeedbackMarker = -1;
    public int[] FeedbackOrder = [];
}
