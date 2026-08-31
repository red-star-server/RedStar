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

    public bool HasPriorityJoin(NetUserId userId)
        => SponsorTierHelpers.HasPriorityJoin(_prototypes, Data);

    public bool TryGetOocColor(NetUserId userId, out Color color)
    {
        if (Data?.OocColor is { } value &&
            SponsorTierHelpers.HasOocColor(_prototypes, Data))
        {
            color = value;
            return true;
        }

        color = default;
        return false;
    }

    public bool TryGetGhostColor(NetUserId userId, out Color color)
    {
        if (Data?.GhostColor is { } value &&
            SponsorTierHelpers.HasGhostColor(_prototypes, Data))
        {
            color = value;
            return true;
        }

        color = default;
        return false;
    }
}
