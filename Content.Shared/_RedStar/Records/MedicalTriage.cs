using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Records;

[Serializable, NetSerializable]
public enum MedicalTriagePriority : byte
{
    None,
    Minor,
    Delayed,
    Immediate,
    Expectant,
}

[Serializable, NetSerializable]
public sealed class MedicalTriageSnapshot
{
    public MedicalTriagePriority Priority { get; }

    public MedicalTriageSnapshot(MedicalTriagePriority priority)
    {
        Priority = priority;
    }
}

[Serializable, NetSerializable]
public sealed class MedicalTriageSetPriorityMessage(MedicalTriagePriority priority, NetEntity? target = null) : BoundUserInterfaceMessage
{
    public MedicalTriagePriority Priority { get; } = priority;
    public NetEntity? Target { get; } = target;
}
