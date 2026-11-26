using Content.Server._RedStar.Genetics.Systems;
using Content.Shared._RedStar.Genetics.Data;
using Content.Server._RedStar.Genetics.Data;
using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RedStar.Genetics.Components;

/// <summary>
/// Reagent-assisted processing of a single acquired gene in the contained patient.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(GeneticChamberSystem), Other = AccessPermissions.Read)]
public sealed partial class GeneticChamberComponent : Component
{
    /// <summary>
    /// Item slot holding the solution container consumed by procedures.
    /// </summary>
    [DataField]
    public string BeakerSlot = "beakerSlot";

    /// <summary>
    /// Item slot holding the vial whose genes the injection procedure adds to the patient.
    /// </summary>
    [DataField]
    public string VialSlot = "vialSlot";

    /// <summary>
    /// Reagent, total dose and duration for each available procedure.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<GeneticProcedure, GeneticProcedureSettings> Procedures = new();

    /// <summary>
    /// Console that started the current procedure; prevents another controller from cancelling it.
    /// </summary>
    public EntityUid? Console;

    /// <summary>
    /// Occupant being processed. Null means the chamber is idle.
    /// </summary>
    public EntityUid? Patient;

    /// <summary>
    /// Original reagent container. Removing or replacing it cancels processing.
    /// </summary>
    public EntityUid? Beaker;

    /// <summary>
    /// Vial captured when an injection started. Replacing it or changing its contents cancels the injection.
    /// </summary>
    public EntityUid? Vial;

    /// <summary>
    /// Vial contents revision captured when an injection started.
    /// </summary>
    public int VialRevision;
    public ProtoId<GenePrototype> Gene;
    public GeneStatus InitialStatus;
    public GeneticProcedure Operation;

    /// <summary>
    /// Dose already consumed, retained until completion or cancellation to avoid rounding drift.
    /// </summary>
    public FixedPoint2 Consumed;

    /// <summary>
    /// Simulation time at which the current procedure started.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan Started;

    /// <summary>
    /// Simulation time at which the current procedure completes.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan Ends;
}

