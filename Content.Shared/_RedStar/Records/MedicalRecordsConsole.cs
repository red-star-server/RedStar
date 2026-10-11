using Content.Shared.StationRecords;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Records;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MedicalRecordsConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public uint? ActiveKey;
}

[Serializable, NetSerializable]
public enum MedicalRecordsConsoleKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MedicalRecordsConsoleState : BoundUserInterfaceState
{
    public Dictionary<uint, string> Listing { get; }
    public uint? SelectedKey { get; }
    public GeneralStationRecord? GeneralRecord { get; }
    public CharacterDossier? Dossier { get; }

    public MedicalRecordsConsoleState(Dictionary<uint, string> listing, uint? selectedKey,
        GeneralStationRecord? generalRecord, CharacterDossier? dossier)
    {
        Listing = listing;
        SelectedKey = selectedKey;
        GeneralRecord = generalRecord;
        Dossier = dossier;
    }
}

[Serializable, NetSerializable]
public sealed class MedicalRecordSelectMessage(uint? key) : BoundUserInterfaceMessage
{
    public uint? Key { get; } = key;
}
