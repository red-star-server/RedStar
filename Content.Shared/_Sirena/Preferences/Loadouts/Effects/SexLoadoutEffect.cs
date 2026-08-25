using System.Diagnostics.CodeAnalysis;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._Sirena.Preferences.Loadouts.Effects;

public sealed partial class SexLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public List<Sex> Sexes = [];

    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        if (Sexes.Contains(profile.Sex))
        {
            reason = null;
            return true;
        }

        reason = FormattedMessage.FromUnformatted(Loc.GetString("loadout-group-sex-restriction"));
        return false;
    }
}
