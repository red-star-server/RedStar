using System.Text.RegularExpressions;

namespace Content.Server._Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    private static readonly Regex UpperCaseWordRegex = new(
        @"(?<![\p{L}\p{N}_+])[А-ЯЁ]{4,}(?![\p{L}\p{N}_+])", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Converts text into valid SSML with pauses and emphasis.
    /// </summary>
    private static string ToSsmlText(string text, SoundTraits traits = NormalTraits)
    {
        text = XmlEscape(WhitespaceRegex.Replace(text, " ").Trim());
        var attrs = BuildProsodyAttributes(traits);

        if (attrs.Length == 0)
        {
            text = UpperCaseWordRegex.Replace(text, match => ShouldEmphasize(match.Value)
                ? $"<prosody pitch=\"high\">{match.Value.ToLowerInvariant()}</prosody>"
                : match.Value);
        }

        return attrs.Length > 0
            ? $"<speak><prosody {attrs}>{text}</prosody></speak>"
            : $"<speak>{text}</speak>";
    }

    private static bool ShouldEmphasize(string word)
    {
        foreach (var c in word)
        {
            if ("АЕЁИОУЫЭЮЯ".Contains(c))
                return true;
        }

        return false;
    }

    private static string XmlEscape(string text)
    {
        return text.Replace("&", "&amp;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\"", "&quot;")
                   .Replace("'", "&apos;");
    }

    private static string BuildProsodyAttributes(SoundTraits traits)
    {
        var attrs = new List<string>();

        if (traits.HasFlag(SoundTraits.RateXSlow))
            attrs.Add("rate=\"x-slow\"");
        else if (traits.HasFlag(SoundTraits.RateSlow))
            attrs.Add("rate=\"slow\"");
        else if (traits.HasFlag(SoundTraits.RateMedium))
            attrs.Add("rate=\"medium\"");
        else if (traits.HasFlag(SoundTraits.RateFast))
            attrs.Add("rate=\"fast\"");
        else if (traits.HasFlag(SoundTraits.RateXFast))
            attrs.Add("rate=\"x-fast\"");

        if (traits.HasFlag(SoundTraits.PitchVerylow))
            attrs.Add("pitch=\"x-low\"");
        else if (traits.HasFlag(SoundTraits.PitchLow))
            attrs.Add("pitch=\"low\"");
        else if (traits.HasFlag(SoundTraits.PitchMedium))
            attrs.Add("pitch=\"medium\"");
        else if (traits.HasFlag(SoundTraits.PitchHigh))
            attrs.Add("pitch=\"high\"");
        else if (traits.HasFlag(SoundTraits.PitchVeryhigh))
            attrs.Add("pitch=\"x-high\"");

        return string.Join(" ", attrs);
    }

    [Flags]
    private enum SoundTraits : ushort
    {
        None = 0,

        RateXSlow = 1 << 0,
        RateSlow = 1 << 1,
        RateMedium = 1 << 2,
        RateFast = 1 << 3,
        RateXFast = 1 << 4,

        PitchVerylow = 1 << 5,
        PitchLow = 1 << 6,
        PitchMedium = 1 << 7,
        PitchHigh = 1 << 8,
        PitchVeryhigh = 1 << 9
    }
}
