using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RedStar.Sponsors;

/// <summary>
/// Restricts a loadout to sponsors whose tier unlocks its prototype.
/// </summary>
public sealed partial class SponsorLoadoutEffect : LoadoutEffect
{
    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        // Validation requiring the actual loadout prototype is handled by RoleLoadout.
        reason = null;
        return true;
    }

    public bool ValidateSponsor(
        ProtoId<LoadoutPrototype> proto,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        if (session == null)
            return true;

        var sponsors = collection.Resolve<ISponsorManager>();
        if (sponsors.HasLoadout(session.UserId, proto))
            return true;

        reason = FormattedMessage.FromUnformatted(Loc.GetString("loadout-sponsor-only"));
        return false;
    }
}
