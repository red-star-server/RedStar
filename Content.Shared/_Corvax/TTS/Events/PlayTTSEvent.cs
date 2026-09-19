using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Corvax.TTS.Events;

[Serializable, NetSerializable]
public enum TTSKind : byte
{
    World = 0,
    Preview = 1,
    Radio = 2
}

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming
public sealed class PlayTTSEvent(
    byte[] data,
    NetEntity? sourceUid = null,
    bool isWhisper = false,
    TTSKind kind = TTSKind.World,
    ProtoId<RadioChannelPrototype>? channel = null)
    : EntityEventArgs
{
    public byte[] Data { get; } = data;
    public NetEntity? SourceUid { get; } = sourceUid;
    public bool IsWhisper { get; } = isWhisper;
    public TTSKind Kind { get; } = kind;
    public ProtoId<RadioChannelPrototype>? Channel { get; } = channel;
}
