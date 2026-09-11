using System.Text.RegularExpressions;
using Content.Server._Corvax.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server._Corvax.Speech.Systems;

public sealed partial class GrowlingAccentSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    private static readonly Regex _regexLowerR = new("r+", RegexOptions.Compiled);
    private static readonly Regex _regexUpperR = new("R+", RegexOptions.Compiled);
    private static readonly Regex _regexLowerRp = new("р+", RegexOptions.Compiled);
    private static readonly Regex _regexUpperRp = new("Р+", RegexOptions.Compiled);
    private static readonly string[] _replacementsR = ["rr", "rrr"];
    private static readonly string[] _replacementsRUpper = ["RR", "RRR"];
    private static readonly string[] _replacementsRp = ["рр", "ррр"];
    private static readonly string[] _replacementsRpUpper = ["РР", "РРР"];

    [SubscribeLocalEvent]
    private void OnAccent(Entity<GrowlingAccentComponent> ent, ref AccentGetEvent args)
    {
        var message = args.Message;

        message = _regexLowerR.Replace(message, _random.Pick(_replacementsR));
        message = _regexUpperR.Replace(message, _random.Pick(_replacementsRUpper));
        message = _regexLowerRp.Replace(message, _random.Pick(_replacementsRp));
        message = _regexUpperRp.Replace(message, _random.Pick(_replacementsRpUpper));

        args.Message = message;
    }
}
