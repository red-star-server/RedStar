using System.Text.RegularExpressions;

namespace Content.Shared._Corvax.TTS;

public static class TTSSpeechStress
{
    public const char Marker = '+';

    private static readonly Regex MarkerRegex = new(
        @"\+(?!\s*[0-9])|(?<![0-9]\s*)\+",
        RegexOptions.Compiled);

    public static string Strip(string message)
    {
        return message.IndexOf(Marker) < 0
            ? message
            : MarkerRegex.Replace(message, string.Empty);
    }
}
