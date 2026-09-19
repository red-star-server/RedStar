using Robust.Shared.Serialization;

namespace Content.Shared._Corvax.TTS.Events;

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeVoiceMessage : BoundUserInterfaceMessage
{
    public string Voice;

    public VoiceMaskChangeVoiceMessage(string voice)
    {
        Voice = voice;
    }
}
