using Content.Shared._RedStar.Sponsors;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client._RedStar.Sponsors;

public sealed partial class ClientSponsorManager : ISponsorManager
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    public SponsorData? Data { get; private set; }

    public void SetData(SponsorData? data)
    {
        Data = data;
    }

    public bool HasLoadout(NetUserId userId, ProtoId<LoadoutPrototype> loadout)
        => SponsorTierHelpers.HasLoadout(_prototypes, Data, loadout);
}
