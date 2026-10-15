namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CellularFusionComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [DataField]
    public TimeSpan SpliceDelay = TimeSpan.FromSeconds(5);

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
