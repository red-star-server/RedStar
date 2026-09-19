using Content.Shared.Inventory;

namespace Content.Shared._Corvax.TTS.Events;

public sealed class TransformSpeakerVoiceEvent(EntityUid sender, string voiceId) : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.MASK;
    public EntityUid Sender = sender;
    public string VoiceId = voiceId;
}
