using Content.Shared._RedStar.Records;

namespace Content.Server._RedStar.Records;

/// <summary>
/// Round-only triage keyed by the native station record ID.
/// </summary>
[RegisterComponent]
public sealed partial class MedicalTriageComponent : Component
{
    public readonly Dictionary<uint, MedicalTriageEntry> Entries = new();
}

public sealed class MedicalTriageEntry
{
    public MedicalTriagePriority Priority;
    public EntityUid? Doctor;
    public string? DoctorName;
}
