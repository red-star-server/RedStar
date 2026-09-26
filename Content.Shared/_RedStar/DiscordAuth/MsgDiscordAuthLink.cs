using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.DiscordAuth;

public sealed class MsgDiscordAuthLink : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public string Link = string.Empty;
    public byte[]? QrCodeBytes;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Link = buffer.ReadString();

        if (!buffer.ReadBoolean())
            return;

        var length = buffer.ReadInt32();
        QrCodeBytes = buffer.ReadBytes(length);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Link);
        buffer.Write(QrCodeBytes != null);

        if (QrCodeBytes == null)
            return;

        buffer.Write(QrCodeBytes.Length);
        buffer.Write(QrCodeBytes);
    }
}
