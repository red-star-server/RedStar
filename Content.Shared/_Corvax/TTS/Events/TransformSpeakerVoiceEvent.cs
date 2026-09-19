using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared._Corvax.TTS.Events;

public sealed class TransformSpeakerVoiceEvent(
    EntityUid sender,
    ProtoId<TTSVoicePrototype> voiceId) : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.MASK;
    public EntityUid Sender = sender;
    public ProtoId<TTSVoicePrototype> VoiceId = voiceId;
}
