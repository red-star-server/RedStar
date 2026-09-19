using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Corvax.TTS;

/// <summary>
/// Shared helpers for selecting and validating TTS voices.
/// </summary>
public static class TTSVoiceHelper
{
    public static readonly ProtoId<TTSVoicePrototype> DefaultVoice = "Garithos";

    public static readonly IReadOnlyDictionary<Sex, ProtoId<TTSVoicePrototype>> DefaultSexVoices =
        new Dictionary<Sex, ProtoId<TTSVoicePrototype>>
        {
            { Sex.Male, "Garithos" },
            { Sex.Female, "Maiev" },
            { Sex.Unsexed, "Myron" }
        };

    public static bool CanUseVoice(
        TTSVoicePrototype voice,
        Sex sex,
        ProtoId<SpeciesPrototype> species)
    {
        if (!voice.RoundStart)
            return false;

        if (voice.SpeciesBlacklist.Contains(species))
            return false;

        if (voice.SpeciesWhitelist.Count > 0 &&
            !voice.SpeciesWhitelist.Contains(species))
        {
            return false;
        }

        if (sex == Sex.Unsexed)
            return true;

        return voice.Sex == Sex.Unsexed || voice.Sex == sex;
    }

    public static IEnumerable<TTSVoicePrototype> GetValidVoices(
        IPrototypeManager prototypeManager,
        Sex sex,
        ProtoId<SpeciesPrototype> species)
    {
        return prototypeManager
            .EnumeratePrototypes<TTSVoicePrototype>()
            .Where(voice => CanUseVoice(voice, sex, species));
    }

    public static ProtoId<TTSVoicePrototype> GetFallbackVoice(
        IPrototypeManager prototypeManager,
        Sex sex,
        ProtoId<SpeciesPrototype> species)
    {
        if (DefaultSexVoices.TryGetValue(sex, out var defaultVoiceId) &&
            prototypeManager.TryIndex(defaultVoiceId, out var defaultVoice) &&
            CanUseVoice(defaultVoice, sex, species))
        {
            return defaultVoiceId;
        }

        if (prototypeManager.TryIndex(DefaultVoice, out var genericDefault) &&
            CanUseVoice(genericDefault, sex, species))
        {
            return DefaultVoice;
        }

        foreach (var voice in GetValidVoices(prototypeManager, sex, species))
        {
            return voice.ID;
        }

        // A species with no usable round-start voices is a content configuration error.
        // Keep a valid prototype ID as the final fallback instead of leaving the profile invalid.
        return DefaultVoice;
    }

    public static ProtoId<TTSVoicePrototype> GetRandomVoice(
        IPrototypeManager prototypeManager,
        IRobustRandom random,
        Sex sex,
        ProtoId<SpeciesPrototype> species)
    {
        var voices = GetValidVoices(prototypeManager, sex, species).ToArray();

        if (voices.Length == 0)
            return GetFallbackVoice(prototypeManager, sex, species);

        return random.Pick(voices).ID;
    }
}
