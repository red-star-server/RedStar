using Content.Shared.Materials;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CellularFusionComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [DataField]
    public ProtoId<MaterialPrototype> RequiredMaterial = "Plasma";

    [DataField]
    public float BaseFailureChance = 0.05f;

    [DataField]
    public float StabilityMultiplier = 0.5f;

    [DataField]
    public float SpliceDelay = 5f;

    [ViewVariables]
    public bool SpliceInProgress => SpliceEndTime != null;

    [ViewVariables, AutoPausedField]
    public TimeSpan? SpliceEndTime;

    [ViewVariables]
    public TimeSpan SpliceDuration;

    [ViewVariables]
    public Cell? SpliceCellA;

    [ViewVariables]
    public Cell? SpliceCellB;

    [ViewVariables]
    public EntityUid? SpliceDish;

    [ViewVariables]
    public int SpliceCost;
}
