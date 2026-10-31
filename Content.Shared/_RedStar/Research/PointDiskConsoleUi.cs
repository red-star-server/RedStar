using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Research;

[Serializable, NetSerializable]
public enum PointDiskConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class PointDiskConsoleState(
    Dictionary<EntProtoId, int> disks,
    int points,
    bool connected,
    bool powered,
    bool printing) : BoundUserInterfaceState
{
    public readonly Dictionary<EntProtoId, int> Disks = disks;
    public readonly int Points = points;
    public readonly bool Connected = connected;
    public readonly bool Powered = powered;
    public readonly bool Printing = printing;
}

[Serializable, NetSerializable]
public sealed class PointDiskConsolePrintMessage(EntProtoId disk) : BoundUserInterfaceMessage
{
    public readonly EntProtoId Disk = disk;
}
