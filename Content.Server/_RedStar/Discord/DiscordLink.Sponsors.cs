using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;

namespace Content.Server.Discord.DiscordLink;

public enum DiscordMemberLookupStatus
{
    Found,
    NotFound,
    Failed
}

public readonly record struct DiscordMemberRolesResult(
    DiscordMemberLookupStatus Status,
    IReadOnlyCollection<ulong>? Roles = null);

public sealed partial class DiscordLink
{
    public event Action<GuildUser>? OnGuildUserUpdated;
    public event Action? OnDiscordReady;

    public void InitializeSponsorTracking()
    {
        if (_client == null)
            return;

        _client.GuildUserUpdate += OnGuildUserUpdateInternal;
        _client.Ready += OnDiscordReadyInternal;
    }

    public void ShutdownSponsorTracking()
    {
        if (_client == null)
            return;

        _client.GuildUserUpdate -= OnGuildUserUpdateInternal;
        _client.Ready -= OnDiscordReadyInternal;
    }

    public async Task<DiscordMemberRolesResult> GetMemberRolesAsync(
        ulong userId,
        CancellationToken cancel = default)
    {
        if (_client == null || _guildId == 0)
            return new(DiscordMemberLookupStatus.Failed);

        try
        {
            var user = await _client.Rest.GetGuildUserAsync(
                _guildId,
                userId,
                cancellationToken: cancel);

            return new(
                DiscordMemberLookupStatus.Found,
                user.RoleIds.ToArray());
        }
        catch (RestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return new(DiscordMemberLookupStatus.NotFound);
        }
        catch (OperationCanceledException)
        {
            return new(DiscordMemberLookupStatus.Failed);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to get Discord roles for user {userId}.", e);
            return new(DiscordMemberLookupStatus.Failed);
        }
    }

    private ValueTask OnGuildUserUpdateInternal(GuildUser user)
    {
        if (user.GuildId == _guildId)
            OnGuildUserUpdated?.Invoke(user);

        return ValueTask.CompletedTask;
    }

    private ValueTask OnDiscordReadyInternal(ReadyEventArgs args)
    {
        OnDiscordReady?.Invoke();
        return ValueTask.CompletedTask;
    }
}
