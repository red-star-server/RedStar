using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery;

[Serializable, NetSerializable]
public sealed partial class SurgeryDoAfterEvent : SimpleDoAfterEvent
{
    public readonly NetEntity Part;
    public readonly EntProtoId Surgery;
    public readonly EntProtoId Step;
    public readonly bool ToolUsed;

    public SurgeryDoAfterEvent(NetEntity part, EntProtoId surgery, EntProtoId step, bool toolUsed)
    {
        Part = part;
        Surgery = surgery;
        Step = step;
        ToolUsed = toolUsed;
    }
}
