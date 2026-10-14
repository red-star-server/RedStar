namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent]
public sealed partial class CellSequencerComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    /// <summary>
    /// Temporary decoding results for the currently inserted dish.
    /// </summary>
    public EntityUid? ActiveDish;
    public int ActiveRevision = -1;
    public HashSet<string> Decoded = [];
    public TimeSpan NextAttempt;
}
