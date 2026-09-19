using System.Linq;
using Content.Shared._Corvax.TTS.Sanitize;
using Robust.Shared.Prototypes;

namespace Content.Server._Corvax.TTS;

public sealed partial class TTSSystem
{
    private readonly List<TTSSanitizePrototype> _sanitizers = new();

    private void InitializeSanitize()
    {
        CacheSanitizers();
    }

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<TTSSanitizePrototype>())
            CacheSanitizers();
    }

    private void CacheSanitizers()
    {
        _sanitizers.Clear();
        _sanitizers.AddRange(ProtoMan
            .EnumeratePrototypes<TTSSanitizePrototype>()
            .Where(proto => proto.Enabled)
            .OrderBy(proto => proto.Priority)
            .ThenBy(proto => proto.ID));
    }

    private string Sanitize(string text)
    {
        foreach (var sanitizer in _sanitizers)
        {
            text = sanitizer.Apply(text);

            if (text.Length == 0)
                return text;
        }

        return text;
    }
}
