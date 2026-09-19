using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._Corvax.TTS;

/// <summary>
/// Prototype represent available TTS voices
/// </summary>
[Prototype("ttsVoice")]
// ReSharper disable once InconsistentNaming
public sealed partial class TTSVoicePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; private set; } = string.Empty;

    [DataField(required: true)]
    public Sex Sex { get; private set; }

    [DataField(required: true)]
    public string Speaker { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the species is available "at round start" (In the character editor)
    /// </summary>
    [DataField]
    public bool RoundStart { get; private set; } = true;

    [DataField]
    public bool SponsorOnly { get; private set; }
}
