using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._RedStar.Sponsors;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Sponsors;

public sealed partial class ServerSponsorManager : ISponsorManager
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IServerDbManager _db = default!;

    private readonly Dictionary<NetUserId, SponsorData> _sponsors = [];

    public async Task<SponsorData?> RefreshAsync(NetUserId userId)
    {
        var record = await _db.GetSponsorAsync(userId);
        if (record == null)
        {
            _sponsors.Remove(userId);
            return null;
        }

        var data = ToShared(record);
        _sponsors[userId] = data;
        return data;
    }

    public void RemoveCached(NetUserId userId)
    {
        _sponsors.Remove(userId);
    }

    public bool TryGetData(NetUserId userId, out SponsorData data)
        => _sponsors.TryGetValue(userId, out data!);

    public async Task<bool> HasPriorityJoinAsync(NetUserId userId)
    {
        if (!_sponsors.TryGetValue(userId, out var data))
        {
            var refreshed = await RefreshAsync(userId);
            if (refreshed == null)
                return false;

            data = refreshed;
        }

        return SponsorTierHelpers.HasPriorityJoin(_prototypes, data);
    }

    public async Task<bool> SetTierAsync(NetUserId userId, string tier)
    {
        if (!_prototypes.HasIndex<SponsorTierPrototype>(tier))
            return false;

        await _db.SetSponsorTierAsync(userId, tier);
        await RefreshAsync(userId);
        return true;
    }

    public async Task RemoveAsync(NetUserId userId)
    {
        await _db.RemoveSponsorAsync(userId);
        _sponsors.Remove(userId);
    }

    public async Task<bool> SetOocColorAsync(NetUserId userId, Color? color)
    {
        var data = await EnsureLoaded(userId);
        if (data == null || !SponsorTierHelpers.HasOocColor(_prototypes, data))
            return false;

        await _db.SetSponsorOocColorAsync(userId, color);
        await RefreshAsync(userId);
        return true;
    }

    public async Task<bool> SetGhostColorAsync(NetUserId userId, Color? color)
    {
        var data = await EnsureLoaded(userId);
        if (data == null || !SponsorTierHelpers.HasGhostColor(_prototypes, data))
            return false;

        await _db.SetSponsorGhostColorAsync(userId, color);
        await RefreshAsync(userId);
        return true;
    }

    public bool HasLoadout(NetUserId userId, ProtoId<LoadoutPrototype> loadout)
        => _sponsors.TryGetValue(userId, out var data) &&
           SponsorTierHelpers.HasLoadout(_prototypes, data, loadout);

    public bool HasPriorityJoin(NetUserId userId)
        => _sponsors.TryGetValue(userId, out var data) &&
           SponsorTierHelpers.HasPriorityJoin(_prototypes, data);

    public bool TryGetOocColor(NetUserId userId, out Color color)
    {
        if (_sponsors.TryGetValue(userId, out var data) &&
            data.OocColor is { } value &&
            SponsorTierHelpers.HasOocColor(_prototypes, data))
        {
            color = value;
            return true;
        }

        color = default;
        return false;
    }

    public bool TryGetGhostColor(NetUserId userId, out Color color)
    {
        if (_sponsors.TryGetValue(userId, out var data) &&
            data.GhostColor is { } value &&
            SponsorTierHelpers.HasGhostColor(_prototypes, data))
        {
            color = value;
            return true;
        }

        color = default;
        return false;
    }

    private async Task<SponsorData?> EnsureLoaded(NetUserId userId)
    {
        if (_sponsors.TryGetValue(userId, out var data))
            return data;

        return await RefreshAsync(userId);
    }

    private static SponsorData ToShared(SponsorRecord record)
    {
        Color? oocColor = null;
        if (record.OocColor != null && Color.TryFromHex(record.OocColor, out var parsedOoc))
            oocColor = parsedOoc;

        Color? ghostColor = null;
        if (record.GhostColor != null && Color.TryFromHex(record.GhostColor, out var parsedGhost))
            ghostColor = parsedGhost;

        return new SponsorData(record.Tier, oocColor, ghostColor);
    }
}
