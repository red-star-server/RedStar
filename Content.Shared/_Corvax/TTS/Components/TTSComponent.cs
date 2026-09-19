using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Corvax.TTS.Components;

/// <summary>
/// Apply TTS for entity chat say messages
/// </summary>
[RegisterComponent, NetworkedComponent]
// ReSharper disable once InconsistentNaming
public sealed partial class TTSComponent : Component
{
    /// <summary>
    /// Prototype of used voice for TTS.
    /// If null, the humanoid profile voice is used.
    /// </summary>
    [DataField("voice")]
    public ProtoId<TTSVoicePrototype>? VoicePrototypeId { get; set; }
}
