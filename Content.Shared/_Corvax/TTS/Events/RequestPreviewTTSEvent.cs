using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Corvax.TTS.Events;

// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public sealed class RequestPreviewTTSEvent(ProtoId<TTSVoicePrototype> voiceId) : EntityEventArgs
{
    public ProtoId<TTSVoicePrototype> VoiceId { get; } = voiceId;
}
