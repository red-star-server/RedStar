using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause(Dirty = true)]
public sealed partial class CellularFusionComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [DataField]
    public TimeSpan SpliceDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Base fraction of the better sample's remaining instability recovered by fusion.
    /// </summary>
    [DataField]
    public float BaseStabilityGain = 0.08f;

    /// <summary>
    /// Additional recovered fraction per unit of the weaker sample's stability.
    /// </summary>
    [DataField]
    public float DonorStabilityGain = 0.04f;

    [ViewVariables]
    public bool SpliceInProgress => SpliceEndTime != null;

    /// <summary>
    /// Completion time in simulation time, shifted when the machine resumes from a pause.
    /// Null while idle; also supplies the client's progress bar.
    /// </summary>
    [ViewVariables, AutoPausedField, AutoNetworkedField]
    public TimeSpan? SpliceEndTime;

    [ViewVariables]
    public int? SpliceSampleA;

    [ViewVariables]
    public int? SpliceSampleB;

    /// <summary>
    /// Dish captured at startup. Removing it cancels fusion.
    /// </summary>
    [ViewVariables]
    public EntityUid? SpliceDish;
}
