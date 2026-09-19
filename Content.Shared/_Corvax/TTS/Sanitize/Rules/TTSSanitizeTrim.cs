namespace Content.Shared._Corvax.TTS.Sanitize.Rules;

/// <summary>
/// Cuts the whitespace off both ends of the phrase
/// </summary>
[DataDefinition]
public sealed partial class TTSSanitizeTrim : ITTSSanitizeRule
{
    public string Apply(string text)
    {
        return text.Trim();
    }
}
