using System.IO;
using Content.Shared._RedStar.DiscordAuth;
using Robust.Client.Graphics;
using Robust.Shared.Network;

namespace Content.Client._RedStar.DiscordAuth;

public sealed partial class DiscordAuthManager
{
    [Dependency] private IClientNetManager _net = default!;

    private DiscordAuthWindow? _window;

    public void Initialize()
    {
        _net.RegisterNetMessage<MsgDiscordAuthLink>(OnLink);
        _net.RegisterNetMessage<MsgDiscordAuthLinked>(OnLinked);
    }

    private void OnLink(MsgDiscordAuthLink msg)
    {
        Texture? qrCode = null;

        if (msg.QrCodeBytes != null)
        {
            using var stream = new MemoryStream(msg.QrCodeBytes);
            qrCode = Texture.LoadFromPNGStream(stream);
        }

        _window?.Close();

        _window = new DiscordAuthWindow(
            msg.Link,
            qrCode,
            CheckStatus);

        _window.OpenCentered();
    }

    private void OnLinked(MsgDiscordAuthLinked msg)
    {
        _window?.SetLinked();
    }

    private void CheckStatus()
    {
        _net.ClientSendMessage(new MsgDiscordAuthCheck());
    }
}
