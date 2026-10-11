using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Records;

[Serializable, NetSerializable]
public sealed class DossierRequestMessage(uint? recordId) : BoundUserInterfaceMessage
{
    public uint? RecordId { get; } = recordId;
}

[Serializable, NetSerializable]
public sealed class DossierStateMessage(CharacterDossier? dossier, DossierSection section) : BoundUserInterfaceMessage
{
    public CharacterDossier? Dossier { get; } = dossier;
    public DossierSection Section { get; } = section;
}
