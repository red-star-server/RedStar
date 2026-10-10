using Content.Shared._CD.Records;
using Content.Shared.Humanoid;

namespace Content.Server._CD.Records;

/// <summary>
/// Server-only RP details indexed by the native station record key. These must not be
/// added to the networked StationRecordsComponent.
/// </summary>
[RegisterComponent]
[Access(typeof(CharacterRecordsSystem))]
public sealed partial class CharacterRecordsComponent : Component
{
    public Dictionary<uint, (PlayerProvidedCharacterRecords Details, Sex Sex, EntityUid Owner)> Records = new();
}
