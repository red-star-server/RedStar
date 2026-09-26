using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._RedStar.DiscordAuth;
using Content.Server.Database;
using Content.Server.Discord.DiscordLink;
using Content.Shared._RedStar.Sponsors;
using NetCord;
using NetCord.Gateway;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
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
    [Dependency] private ITaskManager _taskManager = default!;
    [Dependency] private ILogManager _log = default!;

    private ISawmill _sawmill = default!;
    private readonly Dictionary<NetUserId, SyncGate> _syncGates = new();

    private sealed class SyncGate
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int Users;
    }

    public void PostInject()
    {
        _sawmill = _log.GetSawmill("discord.sponsors");
    }

    public void Initialize()
    {
        _players.PlayerStatusChanged += OnPlayerStatusChanged;
        _auth.Linked += OnDiscordLinked;
        _discord.OnGuildUserUpdated += OnGuildUserUpdated;
        _discord.OnGuildUserRemoved += OnGuildUserRemoved;
        _discord.OnDiscordReady += OnDiscordReady;

        _discord.InitializeSponsorTracking();
    }

    public void Shutdown()
    {
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
        _auth.Linked -= OnDiscordLinked;
        _discord.OnGuildUserUpdated -= OnGuildUserUpdated;
        _discord.OnGuildUserRemoved -= OnGuildUserRemoved;
        _discord.OnDiscordReady -= OnDiscordReady;

        _discord.ShutdownSponsorTracking();
    }

    public async Task SyncPlayerAsync(NetUserId userId)
    {
        await RunForPlayerAsync(userId, () => SyncPlayerCoreAsync(userId));
    }

    private async Task RunForPlayerAsync(NetUserId userId, Func<Task> action)
    {
        SyncGate gate;
        lock (_syncGates)
        {
            if (!_syncGates.TryGetValue(userId, out var existing))
                _syncGates[userId] = existing = new SyncGate();

            gate = existing;
            gate.Users++;
        }

        try
        {
            await gate.Semaphore.WaitAsync();
            try
            {
                await action();
            }
            finally
            {
                gate.Semaphore.Release();
            }
        }
        finally
        {
            lock (_syncGates)
            {
                if (--gate.Users == 0)
                {
                    _syncGates.Remove(userId);
                    gate.Semaphore.Dispose();
                }
            }
        }
    }

    private async Task SyncPlayerCoreAsync(NetUserId userId)
    {
        var discord = await _auth.GetDiscordIdAsync(userId);

        switch (discord.Status)
        {
            case DiscordAuthLookupStatus.NotFound:
                await RemoveDiscordTierAsync(userId);
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

        var current = await _sponsors.RefreshRecordAsync(userId);

        if (current is { DiscordManaged: false })
            return;

        if (selected == null)
        {
            await RemoveDiscordTierAsync(userId, current);
            return;
        }

        if (current?.Tier == selected.ID)
            return;

        if (!await _sponsors.SetTierAsync(userId, selected.ID, discordManaged: true))
        {
            _sawmill.Warning(
                $"Failed to set sponsor tier '{selected.ID}' for {userId}.");
            return;
        }

        await SyncConnectedPlayerAsync(userId);

        _sawmill.Info(
            $"Set sponsor tier '{selected.ID}' for {userId}.");
    }

    private async Task RemoveDiscordTierAsync(NetUserId userId, SponsorRecord? current = null)
    {
        current ??= await _sponsors.RefreshRecordAsync(userId);
        if (current is not { DiscordManaged: true })
            return;

        await _sponsors.RemoveAsync(userId);
        await SyncConnectedPlayerAsync(userId);
        _sawmill.Info($"Removed Discord sponsor tier from {userId}.");
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

    private void OnGuildUserUpdated(GuildUser user)
    {
        var discordId = user.Id;
        _taskManager.RunOnMainThread(async void () =>
        {
            var linked = await _auth.GetUserIdAsync(discordId);

            if (linked.Status != DiscordAuthLookupStatus.Found)
                return;

            await SyncPlayerAsync(linked.UserId);
        });
    }

    private void OnGuildUserRemoved(GuildUserRemoveEventArgs args)
    {
        var discordId = args.User.Id;
        _taskManager.RunOnMainThread(async void () =>
        {
            var linked = await _auth.GetUserIdAsync(discordId);

            if (linked.Status != DiscordAuthLookupStatus.Found)
                return;

            await RunForPlayerAsync(linked.UserId, () => RemoveDiscordTierAsync(linked.UserId));
        });
    }

    private void OnDiscordReady()
    {
        _taskManager.RunOnMainThread(async void () =>
        {
            foreach (var session in _players.Sessions)
            {
                if (session.Status == SessionStatus.Disconnected)
                    continue;

                await SyncPlayerAsync(session.UserId);
            }
        });
    }
}
