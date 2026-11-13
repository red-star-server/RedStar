using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph;

[Serializable, NetSerializable]
public sealed partial class SurgeryGraphStepDoAfterEvent : SimpleDoAfterEvent
{
    public readonly string EdgeId;
    public readonly int StepIndex;

    public SurgeryGraphStepDoAfterEvent(string edgeId, int stepIndex)
    {
        EdgeId = edgeId;
        StepIndex = stepIndex;
    }
}
