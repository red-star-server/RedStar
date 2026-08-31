using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Sponsors;

public interface ISponsorManager
{
    bool HasLoadout(NetUserId userId, ProtoId<LoadoutPrototype> loadout);
}
