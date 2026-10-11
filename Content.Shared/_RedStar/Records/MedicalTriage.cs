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
    public string? DoctorName { get; }

    public MedicalTriageSnapshot(MedicalTriagePriority priority, string? doctorName)
    {
        Priority = priority;
        DoctorName = doctorName;
    }
}

[Serializable, NetSerializable]
public sealed class MedicalTriageSetPriorityMessage(MedicalTriagePriority priority) : BoundUserInterfaceMessage
{
    public MedicalTriagePriority Priority { get; } = priority;
}

[Serializable, NetSerializable]
public sealed class MedicalTriageClaimMessage : BoundUserInterfaceMessage;
