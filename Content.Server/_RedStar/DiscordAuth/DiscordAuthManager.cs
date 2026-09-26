using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._RedStar.DiscordAuth;
using QRCoder;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RedStar.DiscordAuth;

public enum DiscordAuthOpenResult
{
    Opened,
    AlreadyLinked,
    Disabled,
    Disconnected,
    Failed
}

public enum DiscordAuthLookupStatus
{
    Found,
    NotFound,
    Failed
}

public readonly record struct DiscordIdLookupResult(
    DiscordAuthLookupStatus Status,
    ulong DiscordId = 0);

public readonly record struct DiscordUserLookupResult(
    DiscordAuthLookupStatus Status,
    NetUserId UserId = default);

public sealed partial class DiscordAuthManager : IPostInjectInit
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IServerNetManager _net = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private ILogManager _log = default!;

    private readonly HttpClient _http = new();

    private ISawmill _sawmill = default!;

    private bool _enabled;
    private string _apiUrl = string.Empty;

    public event Action<NetUserId>? Linked;

    private enum DiscordLinkStatus
    {
        Linked,
        NotLinked,
        Failed
    }

    public void PostInject()
    {
        _sawmill = _log.GetSawmill("discord.auth");
    }

    public void Initialize()
    {
        _cfg.OnValueChanged(
            DiscordAuthCVars.Enabled,
            value => _enabled = value,
            true);

        _cfg.OnValueChanged(
            DiscordAuthCVars.ApiUrl,
            value => _apiUrl = value.TrimEnd('/'),
            true);

        _cfg.OnValueChanged(
            DiscordAuthCVars.ApiKey,
            OnApiKeyChanged,
            true);

        _net.RegisterNetMessage<MsgDiscordAuthLink>();
        _net.RegisterNetMessage<MsgDiscordAuthLinked>();
        _net.RegisterNetMessage<MsgDiscordAuthCheck>(OnAuthCheck);

        _players.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public void Shutdown()
    {
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
        _http.Dispose();
    }

    public async Task<DiscordAuthOpenResult> OpenLinkAsync(
        ICommonSession session,
        CancellationToken cancel = default)
    {
        if (!_enabled)
            return DiscordAuthOpenResult.Disabled;

        var status = await GetLinkStatusAsync(session.UserId, cancel);

        switch (status)
        {
            case DiscordLinkStatus.Linked:
                return DiscordAuthOpenResult.AlreadyLinked;

            case DiscordLinkStatus.Failed:
                return DiscordAuthOpenResult.Failed;

            case DiscordLinkStatus.NotLinked:
                break;
        }

        var link = await GetLinkAsync(session.UserId, cancel);
        if (link == null)
        {
            _sawmill.Warning($"Failed to get Discord auth link for {session.UserId}.");
            return DiscordAuthOpenResult.Failed;
        }

        if (!session.Channel.IsConnected)
            return DiscordAuthOpenResult.Disconnected;

        _net.ServerSendMessage(
            new MsgDiscordAuthLink
            {
                Link = link,
                QrCodeBytes = GenerateQrCode(link)
            },
            session.Channel);

        return DiscordAuthOpenResult.Opened;
    }

    public async Task<DiscordIdLookupResult> GetDiscordIdAsync(
        NetUserId userId,
        CancellationToken cancel = default)
    {
        if (!_enabled)
            return new DiscordIdLookupResult(DiscordAuthLookupStatus.Failed);

        if (!IsApiUrlValid())
            return new DiscordIdLookupResult(DiscordAuthLookupStatus.Failed);

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_apiUrl}/identify?method=uid&id={userId}");

            using var response = await _http.SendAsync(request, cancel);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new DiscordIdLookupResult(DiscordAuthLookupStatus.NotFound);

            if (!response.IsSuccessStatusCode)
            {
                _sawmill.Warning(
                    $"Unexpected Discord auth response while resolving Discord ID for {userId}: " +
                    $"{(int) response.StatusCode} {response.StatusCode}");

                return new DiscordIdLookupResult(DiscordAuthLookupStatus.Failed);
            }

            var data = await response.Content.ReadFromJsonAsync<DiscordIdentifyResponse>(
                cancellationToken: cancel);

            if (data?.Id != null && ulong.TryParse(data.Id, out var discordId))
            {
                return new DiscordIdLookupResult(
                    DiscordAuthLookupStatus.Found,
                    discordId);
            }

            _sawmill.Warning(
                $"Discord auth service returned an invalid Discord ID for {userId}.");

            return new DiscordIdLookupResult(DiscordAuthLookupStatus.Failed);
        }
        catch (HttpRequestException e)
        {
            _sawmill.Error(
                $"Failed to resolve Discord ID for {userId}: {e.Message}");

            return new DiscordIdLookupResult(DiscordAuthLookupStatus.Failed);
        }
        catch (JsonException e)
        {
            _sawmill.Error(
                $"Discord auth service returned invalid JSON while resolving Discord ID for {userId}: {e.Message}");

            return new DiscordIdLookupResult(DiscordAuthLookupStatus.Failed);
        }
        catch (NotSupportedException e)
        {
            _sawmill.Error(
                $"Discord auth service returned an unsupported response while resolving Discord ID for {userId}: {e.Message}");

            return new DiscordIdLookupResult(DiscordAuthLookupStatus.Failed);
        }
        catch (OperationCanceledException e)
        {
            _sawmill.Warning(
                $"Discord ID lookup was cancelled or timed out for {userId}: {e.Message}");

            return new DiscordIdLookupResult(DiscordAuthLookupStatus.Failed);
        }
    }

    public async Task<DiscordUserLookupResult> GetUserIdAsync(
        ulong discordId,
        CancellationToken cancel = default)
    {
        if (!_enabled)
            return new DiscordUserLookupResult(DiscordAuthLookupStatus.Failed);

        if (!IsApiUrlValid())
            return new DiscordUserLookupResult(DiscordAuthLookupStatus.Failed);

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_apiUrl}/uuid?method=discord&id={discordId}");

            using var response = await _http.SendAsync(request, cancel);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new DiscordUserLookupResult(DiscordAuthLookupStatus.NotFound);

            if (!response.IsSuccessStatusCode)
            {
                _sawmill.Warning(
                    $"Unexpected Discord auth response while resolving SS14 account for Discord user {discordId}: " +
                    $"{(int) response.StatusCode} {response.StatusCode}");

                return new DiscordUserLookupResult(DiscordAuthLookupStatus.Failed);
            }

            var data = await response.Content.ReadFromJsonAsync<DiscordUuidResponse>(
                cancellationToken: cancel);

            if (data?.Uuid != null && Guid.TryParse(data.Uuid, out var guid))
            {
                return new DiscordUserLookupResult(
                    DiscordAuthLookupStatus.Found,
                    new NetUserId(guid));
            }

            _sawmill.Warning(
                $"Discord auth service returned an invalid SS14 UUID for Discord user {discordId}.");

            return new DiscordUserLookupResult(DiscordAuthLookupStatus.Failed);
        }
        catch (HttpRequestException e)
        {
            _sawmill.Error(
                $"Failed to resolve SS14 account for Discord user {discordId}: {e.Message}");

            return new DiscordUserLookupResult(DiscordAuthLookupStatus.Failed);
        }
        catch (JsonException e)
        {
            _sawmill.Error(
                $"Discord auth service returned invalid JSON while resolving SS14 account for Discord user {discordId}: {e.Message}");

            return new DiscordUserLookupResult(DiscordAuthLookupStatus.Failed);
        }
        catch (NotSupportedException e)
        {
            _sawmill.Error(
                $"Discord auth service returned an unsupported response while resolving SS14 account for Discord user {discordId}: {e.Message}");

            return new DiscordUserLookupResult(DiscordAuthLookupStatus.Failed);
        }
        catch (OperationCanceledException e)
        {
            _sawmill.Warning(
                $"SS14 account lookup was cancelled or timed out for Discord user {discordId}: {e.Message}");

            return new DiscordUserLookupResult(DiscordAuthLookupStatus.Failed);
        }
    }

    private async Task<DiscordLinkStatus> GetLinkStatusAsync(
        NetUserId userId,
        CancellationToken cancel = default)
    {
        if (!IsApiUrlValid())
            return DiscordLinkStatus.Failed;

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_apiUrl}/uuid?method=uid&id={userId}");

            using var response = await _http.SendAsync(request, cancel);

            if (response.StatusCode == HttpStatusCode.OK)
                return DiscordLinkStatus.Linked;

            if (response.StatusCode == HttpStatusCode.NotFound)
                return DiscordLinkStatus.NotLinked;

            _sawmill.Warning(
                $"Unexpected Discord auth response while checking {userId}: " +
                $"{(int) response.StatusCode} {response.StatusCode}");

            return DiscordLinkStatus.Failed;
        }
        catch (HttpRequestException e)
        {
            _sawmill.Error($"Discord auth service is unavailable: {e.Message}");
            return DiscordLinkStatus.Failed;
        }
        catch (OperationCanceledException e)
        {
            _sawmill.Warning(
                $"Discord auth status request was cancelled or timed out: {e.Message}");

            return DiscordLinkStatus.Failed;
        }
    }

    private async Task<string?> GetLinkAsync(
        NetUserId userId,
        CancellationToken cancel = default)
    {
        try
        {
            using var response = await _http.GetAsync(
                $"{_apiUrl}/link?uid={userId}",
                cancel);

            if (!response.IsSuccessStatusCode)
            {
                _sawmill.Warning(
                    $"Discord auth service returned {(int) response.StatusCode} " +
                    $"{response.StatusCode} while generating a link for {userId}.");

                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<DiscordLinkResponse>(
                cancellationToken: cancel);

            if (data?.Link is { } link &&
                Uri.TryCreate(link, UriKind.Absolute, out var linkUri) &&
                linkUri.Scheme is "http" or "https")
                return link;

            _sawmill.Warning(
                $"Discord auth service returned an invalid link for {userId}.");

            return null;
        }
        catch (HttpRequestException e)
        {
            _sawmill.Error(
                $"Failed to request Discord auth link for {userId}: {e.Message}");

            return null;
        }
        catch (JsonException e)
        {
            _sawmill.Error(
                $"Discord auth service returned invalid JSON for {userId}: {e.Message}");

            return null;
        }
        catch (NotSupportedException e)
        {
            _sawmill.Error(
                $"Discord auth service returned an unsupported response for {userId}: {e.Message}");

            return null;
        }
        catch (OperationCanceledException e)
        {
            _sawmill.Warning(
                $"Discord auth link request was cancelled or timed out for {userId}: {e.Message}");

            return null;
        }
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.Connected)
            return;

        if (!_enabled)
            return;

        var result = await OpenLinkAsync(args.Session);

        if (result == DiscordAuthOpenResult.Failed)
        {
            _sawmill.Warning(
                $"Failed to automatically open Discord auth for {args.Session.UserId}.");
        }
    }

    private async void OnAuthCheck(MsgDiscordAuthCheck msg)
    {
        var status = await GetLinkStatusAsync(msg.MsgChannel.UserId);

        if (status != DiscordLinkStatus.Linked)
            return;

        _net.ServerSendMessage(
            new MsgDiscordAuthLinked(),
            msg.MsgChannel);

        Linked?.Invoke(msg.MsgChannel.UserId);
    }

    private bool IsApiUrlValid()
    {
        if (Uri.TryCreate(_apiUrl, UriKind.Absolute, out var apiUri) &&
            apiUri.Scheme is "http" or "https")
            return true;

        _sawmill.Warning(
            "Discord auth is enabled, but API URL is not a valid HTTP(S) URL.");

        return false;
    }

    private void OnApiKeyChanged(string value)
    {
        _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(value)
                ? null
                : new AuthenticationHeaderValue("Bearer", value);
    }

    private byte[]? GenerateQrCode(string link)
    {
        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(
                link,
                QRCodeGenerator.ECCLevel.Q);

            using var qr = new PngByteQRCode(data);

            return qr.GetGraphic(8);
        }
        catch (Exception e)
        {
            _sawmill.Error(
                $"Failed to generate Discord auth QR code: {e.Message}");

            return null;
        }
    }

    private sealed record DiscordLinkResponse(string Link);
    private sealed record DiscordIdentifyResponse(string Id);
    private sealed record DiscordUuidResponse(string Uuid);
}
