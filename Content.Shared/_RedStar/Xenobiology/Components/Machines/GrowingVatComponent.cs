using Content.Shared._RedStar.Genetics;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

[RegisterComponent]
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
    public string? Recipe;

    [ViewVariables]
    public Dictionary<ProtoId<GenePrototype>, GeneState> InputStates = [];

    [ViewVariables]
    public TimeSpan Progress;

    [ViewVariables]
    public float TickAccumulator;

    [ViewVariables]
    public bool Processing;

    public EntityUid? AudioStream;
}
