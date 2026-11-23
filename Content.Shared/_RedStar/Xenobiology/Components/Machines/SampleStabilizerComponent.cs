using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

/// <summary>
/// Restores the stability of one sample by consuming a reagent from an inserted beaker.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause(Dirty = true)]
public sealed partial class SampleStabilizerComponent : Component
{
    /// <summary>
    /// Item slot holding the Petri dish with the samples.
    /// </summary>
    [DataField]
    public string DishSlot = "dishSlot";

    /// <summary>
    /// Item slot holding the beaker that supplies the stabilizing reagent.
    /// </summary>
    [DataField]
    public string BeakerSlot = "beakerSlot";

    /// <summary>
    /// Reagent consumed by stabilization.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> Reagent = "Cryoxadone";

    /// <summary>
    /// Stability, from zero to one, restored by each unit of reagent.
    /// </summary>
    [DataField]
    public float StabilityPerUnit = 0.05f;

    /// <summary>
    /// Largest stability gain of one stabilization cycle.
    /// </summary>
    [DataField]
    public float MaximumGain = 0.25f;

    /// <summary>
    /// Duration of one stabilization cycle.
    /// </summary>
    [DataField]
    public TimeSpan StabilizationDelay = TimeSpan.FromSeconds(5);

    [ViewVariables]
    public bool StabilizationInProgress => StabilizationEndTime != null;

    /// <summary>
    /// Completion time in simulation time, shifted when the machine resumes from a pause.
    /// Null while idle; also supplies the client's progress bar.
    /// </summary>
    [ViewVariables, AutoPausedField, AutoNetworkedField]
    public TimeSpan? StabilizationEndTime;

    /// <summary>
    /// Sample captured at startup.
    /// </summary>
    [ViewVariables]
    public int? StabilizationSample;

    /// <summary>
    /// Dish captured at startup. Removing it cancels stabilization.
    /// </summary>
    [ViewVariables]
    public EntityUid? StabilizationDish;

    /// <summary>
    /// Beaker captured at startup. Removing it cancels stabilization.
    /// </summary>
    [ViewVariables]
    public EntityUid? StabilizationBeaker;
}
