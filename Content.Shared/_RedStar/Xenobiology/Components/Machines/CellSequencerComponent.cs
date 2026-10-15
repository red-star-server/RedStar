using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent]
public sealed partial class CellSequencerComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [DataField]
    public string VialSlot = "vialSlot";

    public EntityUid? ActiveDish;
    public int ActiveRevision = -1;
    public HashSet<(ProtoId<GenePrototype> Gene, string? Allele, int? Number)> Decoded = [];
    public int FeedbackSample = -1;
    public int FeedbackMarker = -1;
    public int[] FeedbackOrder = [];
}
