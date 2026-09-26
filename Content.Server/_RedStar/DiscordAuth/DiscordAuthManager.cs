using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._RedStar.DiscordAuth;
using QRCoder;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RedStar.DiscordAuth;

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
    }

    public void Shutdown()
    {
        _http.Dispose();
    }

    public async Task OpenLinkAsync(
        ICommonSession session,
        CancellationToken cancel = default)
    {
        if (!_enabled)
            return;

        if (await IsLinkedAsync(session.UserId, cancel))
        {
            _net.ServerSendMessage(new MsgDiscordAuthLinked(), session.Channel);
            return;
        }

        var link = await GetLinkAsync(session.UserId, cancel);
        if (link == null)
        {
            _sawmill.Warning($"Failed to get Discord auth link for {session.UserId}.");
            return;
        }

        _net.ServerSendMessage(
            new MsgDiscordAuthLink
            {
                Link = link,
                QrCodeBytes = GenerateQrCode(link)
            },
            session.Channel);
    }

    public async Task<bool> IsLinkedAsync(
        NetUserId userId,
        CancellationToken cancel = default)
    {
        if (!_enabled || string.IsNullOrWhiteSpace(_apiUrl))
            return false;

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_apiUrl}/uuid?method=uid&id={userId}");

            using var response = await _http.SendAsync(request, cancel);

            return response.StatusCode switch
            {
                HttpStatusCode.OK => true,
                HttpStatusCode.NotFound => false,
                _ => false
            };
        }
        catch (HttpRequestException e)
        {
            _sawmill.Error($"Discord auth service is unavailable: {e.Message}");
            return false;
        }
    }

    private async Task<string?> GetLinkAsync(
        NetUserId userId,
        CancellationToken cancel)
    {
        try
        {
            using var response = await _http.GetAsync(
                $"{_apiUrl}/link?uid={userId}",
                cancel);

            if (!response.IsSuccessStatusCode)
                return null;

            var data = await response.Content.ReadFromJsonAsync<DiscordLinkResponse>(
                cancellationToken: cancel);

            return data?.Link;
        }
        catch (HttpRequestException e)
        {
            _sawmill.Error($"Failed to request Discord auth link: {e.Message}");
            return null;
        }
    }

    private async void OnAuthCheck(MsgDiscordAuthCheck msg)
    {
        if (!await IsLinkedAsync(msg.MsgChannel.UserId))
            return;

        if (!_players.TryGetSessionById(msg.MsgChannel.UserId, out var session))
            return;

        _net.ServerSendMessage(new MsgDiscordAuthLinked(), session.Channel);
    }

    private void OnApiKeyChanged(string value)
    {
        _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(value)
                ? null
                : new AuthenticationHeaderValue("Bearer", value);
    }

    private static byte[]? GenerateQrCode(string link)
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
        catch
        {
            return null;
        }
    }

    private sealed record DiscordLinkResponse(string Link);
}
