using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery;

[Serializable, NetSerializable]
public enum SurgeryUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class SurgeryStepChosenBuiMsg(NetEntity part, EntProtoId surgery, EntProtoId step) : BoundUserInterfaceMessage
{
    public readonly NetEntity Part = part;
    public readonly EntProtoId Surgery = surgery;
    public readonly EntProtoId Step = step;
}
