using Content.Shared.StationRecords;

namespace Content.Server._RedStar.Records;

/// <summary>
/// Server-only round copies of character dossiers, indexed by native station record IDs.
/// </summary>
[RegisterComponent]
[Access(typeof(CharacterDossierSystem))]
public sealed partial class CharacterDossierComponent : Component
{
    public StationRecordSet Records = new();
}
