using Content.Shared.Materials;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent]
public sealed partial class CellSequencerComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [DataField]
    public ProtoId<MaterialPrototype> RequiredMaterial = "Plasma";
}
