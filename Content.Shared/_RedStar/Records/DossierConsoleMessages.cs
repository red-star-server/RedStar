using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Records;

[Serializable, NetSerializable]
public sealed class DossierRequestMessage(uint? recordId) : BoundUserInterfaceMessage
{
    public uint? RecordId { get; } = recordId;
}

[Serializable, NetSerializable]
public sealed class DossierStateMessage(CharacterDossier? dossier) : BoundUserInterfaceMessage
{
    public CharacterDossier? Dossier { get; } = dossier;
}
