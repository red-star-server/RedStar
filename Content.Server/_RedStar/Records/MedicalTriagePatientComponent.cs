using Content.Shared.StationRecords;

namespace Content.Server._RedStar.Records;

/// <summary>
/// The station record assigned to this character when they spawned.
/// </summary>
[RegisterComponent]
public sealed partial class MedicalTriagePatientComponent : Component
{
    public StationRecordKey Key;
}
