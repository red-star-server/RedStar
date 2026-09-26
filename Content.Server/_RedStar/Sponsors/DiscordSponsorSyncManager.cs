using System.Linq;
using System.Threading.Tasks;
using Content.Server._RedStar.DiscordAuth;
using Content.Server.Discord.DiscordLink;
using Content.Shared._RedStar.Sponsors;
using NetCord;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Sponsors;

public sealed partial class DiscordSponsorSyncManager : IPostInjectInit
{
    [Dependency] private DiscordAuthManager _auth = default!;
    [Dependency] private DiscordLink _discord = default!;
    [Dependency] private ServerSponsorManager _sponsors = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private ILogManager _log = default!;

    private ISawmill _sawmill = default!;

    public void PostInject()
    {
        _sawmill = _log.GetSawmill("discord.sponsors");
    }

    public void Initialize()
    {
        _players.PlayerStatusChanged += OnPlayerStatusChanged;
        _auth.Linked += OnDiscordLinked;
        _discord.OnGuildUserUpdated += OnGuildUserUpdated;
        _discord.OnDiscordReady += OnDiscordReady;

        _discord.InitializeSponsorTracking();
    }

    public void Shutdown()
    {
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
        _auth.Linked -= OnDiscordLinked;
        _discord.OnGuildUserUpdated -= OnGuildUserUpdated;
        _discord.OnDiscordReady -= OnDiscordReady;

        _discord.ShutdownSponsorTracking();
    }

    public async Task SyncPlayerAsync(NetUserId userId)
    {
        var discord = await _auth.GetDiscordIdAsync(userId);

        switch (discord.Status)
        {
            case DiscordAuthLookupStatus.NotFound:
                return;

            case DiscordAuthLookupStatus.Failed:
                return;

            case DiscordAuthLookupStatus.Found:
                break;
        }

        var member = await _discord.GetMemberRolesAsync(discord.DiscordId);

        switch (member.Status)
        {
            case DiscordMemberLookupStatus.NotFound:
                await ApplyRolesAsync(userId, []);
                return;

            case DiscordMemberLookupStatus.Failed:
                return;

            case DiscordMemberLookupStatus.Found:
                break;
        }

        await ApplyRolesAsync(userId, member.Roles ?? []);
    }

    private async Task ApplyRolesAsync(
        NetUserId userId,
        IReadOnlyCollection<ulong> roles)
    {
        SponsorTierPrototype? selected = null;
        var selectedDepth = -1;

        foreach (var tier in _prototypes.EnumeratePrototypes<SponsorTierPrototype>())
        {
            if (tier.DiscordRoleId is not { } roleId || !roles.Contains(roleId))
                continue;

            var depth = GetTierDepth(tier);

            if (depth <= selectedDepth)
                continue;

            selected = tier;
            selectedDepth = depth;
        }

        var current = await _sponsors.RefreshAsync(userId);

        if (selected == null)
        {
            if (current == null)
                return;

            await _sponsors.RemoveAsync(userId);
            await SyncConnectedPlayerAsync(userId);

            _sawmill.Info($"Removed sponsor tier from {userId}.");
            return;
        }

        if (current?.Tier == selected.ID)
            return;

        if (!await _sponsors.SetTierAsync(userId, selected.ID))
        {
            _sawmill.Warning(
                $"Failed to set sponsor tier '{selected.ID}' for {userId}.");
            return;
        }

        await SyncConnectedPlayerAsync(userId);

        _sawmill.Info(
            $"Set sponsor tier '{selected.ID}' for {userId}.");
    }

    private int GetTierDepth(SponsorTierPrototype tier)
    {
        var depth = 0;
        var current = tier;
        var visited = new HashSet<string>();

        while (visited.Add(current.ID) &&
               current.Parent is { } parent &&
               _prototypes.TryIndex(parent, out SponsorTierPrototype? parentTier))
        {
            depth++;
            current = parentTier;
        }

        return depth;
    }

    private async Task SyncConnectedPlayerAsync(NetUserId userId)
    {
        if (!_players.TryGetSessionById(userId, out var session) ||
            session.Status == SessionStatus.Disconnected)
            return;

        await _entities.System<SponsorSystem>().SyncPlayerAsync(userId);
    }

    private async void OnPlayerStatusChanged(
        object? sender,
        SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.Connected)
            return;

        await SyncPlayerAsync(args.Session.UserId);
    }

    private async void OnDiscordLinked(NetUserId userId)
    {
        await SyncPlayerAsync(userId);
    }

    private async void OnGuildUserUpdated(GuildUser user)
    {
        var linked = await _auth.GetUserIdAsync(user.Id);

        if (linked.Status != DiscordAuthLookupStatus.Found)
            return;

        await ApplyRolesAsync(
            linked.UserId,
            user.RoleIds.ToArray());
    }

    private async void OnDiscordReady()
    {
        foreach (var session in _players.Sessions)
        {
            if (session.Status == SessionStatus.Disconnected)
                continue;

            await SyncPlayerAsync(session.UserId);
        }
    }
}
