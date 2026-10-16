using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared._RedStar.Genetics.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class GrowingVatComponent : Component
{
    [DataField]
    public string Solution = "vat";

    [DataField]
    public string SubjectSlot = "subjectSlot";

    [DataField]
    public SoundSpecifier? ProcessSound;

    [ViewVariables]
    public EntityUid? Subject;

    [ViewVariables]
    public ProtoId<GeneticMutationPrototype>? Recipe;

    [ViewVariables, AutoPausedField]
    public TimeSpan? EndTime;

    public bool NeedsEvaluation = true;

    [ViewVariables]
    public bool Processing;

    public EntityUid? AudioStream;
}
