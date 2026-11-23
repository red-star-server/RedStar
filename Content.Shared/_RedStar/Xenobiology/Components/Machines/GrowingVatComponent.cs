using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared._RedStar.Genetics.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class GrowingVatComponent : Component
{
    /// <summary>
    /// Name of the solution that holds the reagents.
    /// </summary>
    [DataField]
    public string Solution = "vat";

    /// <summary>
    /// Container slot holding the subject.
    /// </summary>
    [DataField]
    public string SubjectSlot = "subjectSlot";

    /// <summary>
    /// Sound played in a loop during processing.
    /// </summary>
    [DataField]
    public SoundSpecifier? ProcessSound;

    [ViewVariables]
    public EntityUid? Subject;

    /// <summary>
    /// Recipe selected for the current operation. Eligibility is rechecked before reagents are spent.
    /// </summary>
    [ViewVariables]
    public ProtoId<GeneticMutationPrototype>? Recipe;

    /// <summary>
    /// Completion time in simulation time, shifted after a pause. Null while idle.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan? EndTime;

    /// <summary>
    /// Requests a recipe and subject reevaluation on the next unpaused update.
    /// </summary>
    public bool NeedsEvaluation;

    /// <summary>
    /// Last processing state applied to appearance and looping audio.
    /// </summary>
    [ViewVariables]
    public bool Processing;

    public EntityUid? AudioStream;
}
