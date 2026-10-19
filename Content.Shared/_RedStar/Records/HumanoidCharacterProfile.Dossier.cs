using Content.Shared._RedStar.Records;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    public HumanoidCharacterProfile WithDossier(CharacterDossier dossier) =>
        new(this) { Dossier = dossier.DeepCopy() };
}
