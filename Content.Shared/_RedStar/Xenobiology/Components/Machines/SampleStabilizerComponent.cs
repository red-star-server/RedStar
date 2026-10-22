using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause(Dirty = true)]
public sealed partial class SampleStabilizerComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [DataField]
    public TimeSpan StabilizationDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Base fraction of the better sample's remaining instability recovered by stabilization.
    /// </summary>
    [DataField]
    public float BaseStabilityGain = 0.08f;

    /// <summary>
    /// Additional recovered fraction per unit of the weaker sample's stability.
    /// </summary>
    [DataField]
    public float DonorStabilityGain = 0.04f;

    [ViewVariables]
    public bool StabilizationInProgress => StabilizationEndTime != null;

    /// <summary>
    /// Completion time in simulation time, shifted when the machine resumes from a pause.
    /// Null while idle; also supplies the client's progress bar.
    /// </summary>
    [ViewVariables, AutoPausedField, AutoNetworkedField]
    public TimeSpan? StabilizationEndTime;

    [ViewVariables]
    public int? StabilizationSampleA;

    [ViewVariables]
    public int? StabilizationSampleB;

    /// <summary>
    /// Dish captured at startup. Removing it cancels stabilization.
    /// </summary>
    [ViewVariables]
    public EntityUid? StabilizationDish;
}
