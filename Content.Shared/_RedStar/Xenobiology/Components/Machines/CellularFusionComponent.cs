namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CellularFusionComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [DataField]
    public float SpliceDelay = 5f;

    [ViewVariables]
    public bool SpliceInProgress => SpliceEndTime != null;

    [ViewVariables, AutoPausedField]
    public TimeSpan? SpliceEndTime;

    [ViewVariables]
    public BiologicalSample? SpliceSampleA;

    [ViewVariables]
    public BiologicalSample? SpliceSampleB;

    [ViewVariables]
    public EntityUid? SpliceDish;
}
