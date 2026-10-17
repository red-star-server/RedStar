namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CellularFusionComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [DataField]
    public TimeSpan SpliceDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public float BaseStabilityGain = 0.08f;

    [DataField]
    public float DonorStabilityGain = 0.04f;

    [ViewVariables]
    public bool SpliceInProgress => SpliceEndTime != null;

    [ViewVariables, AutoPausedField]
    public TimeSpan? SpliceEndTime;

    [ViewVariables]
    public int? SpliceSampleA;

    [ViewVariables]
    public int? SpliceSampleB;

    [ViewVariables]
    public EntityUid? SpliceDish;

    [ViewVariables]
    public EntityUid? SpliceUser;
}
