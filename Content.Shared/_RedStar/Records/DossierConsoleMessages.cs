using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Records;

[Serializable, NetSerializable]
public sealed class DossierRequestMessage(uint? recordId) : BoundUserInterfaceMessage
{
    public uint? RecordId { get; } = recordId;
}

[Serializable, NetSerializable]
public sealed class DossierStateMessage(uint? recordId, CharacterDossier? dossier) : BoundUserInterfaceMessage
{
    public uint? RecordId { get; } = recordId;
    public CharacterDossier? Dossier { get; } = dossier;
}
