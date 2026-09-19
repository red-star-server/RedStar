using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Corvax.TTS;

/// <summary>
/// Voice available for speech synthesis.
/// </summary>
[Prototype("ttsVoice")]
// ReSharper disable once InconsistentNaming
public sealed partial class TTSVoicePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Description shown for the voice.
    /// </summary>
    [DataField]
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Category shown in the voice selector.
    /// </summary>
    [DataField]
    public LocId Category { get; private set; } = "humanoid-profile-editor-voice-other";

    /// <summary>
    /// Sex this voice is intended for.
    /// </summary>
    [DataField(required: true)]
    public Sex Sex { get; private set; }

    /// <summary>
    /// Voice identifier used by the TTS service.
    /// </summary>
    [DataField(required: true)]
    public string Speaker { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the voice is available in the character editor.
    /// </summary>
    [DataField]
    public bool RoundStart { get; private set; } = true;

    /// <summary>
    /// Species that cannot use this voice.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> SpeciesBlacklist { get; private set; } = new();

    /// <summary>
    /// If not empty, only these species can use this voice.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> SpeciesWhitelist { get; private set; } = new();
}
