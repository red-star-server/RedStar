using Content.Shared._RedStar.Genetics.Data;
using Content.Server._RedStar.Genetics.Data;
using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RedStar.Genetics.Components;

/// <summary>
/// Reagent-assisted processing of a single acquired gene in the contained patient.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class GeneticChamberComponent : Component
{
    /// <summary>
    /// Item slot holding the solution container consumed by procedures.
    /// </summary>
    [DataField]
    public string BeakerSlot = "beakerSlot";

    /// <summary>
    /// Reagent, total dose and duration for each available procedure.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<GeneticProcedure, GeneticProcedureSettings> Procedures = new();

    /// <summary>
    /// Audible reagent delivery during an active procedure.
    /// </summary>
    [DataField]
    public SoundSpecifier? InjectionSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    [DataField]
    public TimeSpan InjectionSoundInterval = TimeSpan.FromSeconds(4);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSound;

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
    public ProtoId<GenePrototype> Gene;
    public GeneStatus InitialStatus;
    public GeneticProcedure Operation;

    /// <summary>
    /// Dose already consumed, retained until completion or cancellation to avoid rounding drift.
    /// </summary>
    public FixedPoint2 Consumed;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan Started;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan Ends;
}

