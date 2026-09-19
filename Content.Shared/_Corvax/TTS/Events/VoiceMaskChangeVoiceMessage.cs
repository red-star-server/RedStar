using Robust.Shared.Serialization;

namespace Content.Shared._Corvax.TTS.Events;

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeVoiceMessage(string voice) : BoundUserInterfaceMessage
{
    public string Voice = voice;
}
