using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Corvax.TTS.Events;

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeVoiceMessage(ProtoId<TTSVoicePrototype> voice) : BoundUserInterfaceMessage
{
    public ProtoId<TTSVoicePrototype> Voice = voice;
}
