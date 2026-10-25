using Robust.Shared.Prototypes;
using Content.Shared.Speech;

namespace Content.Shared._Trauma.Speech;

public sealed partial class OhioAccentSystem : EntitySystem
{
    [Dependency] private SimpleAccentSystem _accent = default!;

    private static readonly ProtoId<SimpleAccentPrototype> Accent = "Ohio";

    [SubscribeLocalEvent]
    private void OnAccentGet(Entity<OhioAccentComponent> ent, ref AccentGetEvent args)
    {
        _accent.ApplyAccent(Accent, ref args);
    }
}
