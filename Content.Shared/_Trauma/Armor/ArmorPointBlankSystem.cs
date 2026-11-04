using Content.Shared._Trauma.Common.Armor;
using Content.Shared.Armor;

namespace Content.Shared._Trauma.Armor;

public sealed partial class ArmorPointBlankSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnProtectAttempt(Entity<ArmorPointBlankComponent> ent, ref ArmorProtectAttemptEvent args)
    {
        if (!args.IsPreciseHit || args.Multiplier <= 0f || ent.Comp.Range <= 0f || args.Origin is not { } origin)
            return;

        if (_transform.InRange(ent.Owner, origin, ent.Comp.Range))
            args.Multiplier *= ent.Comp.ResistancePenalty;
    }

    [SubscribeLocalEvent]
    private void OnArmorExamine(Entity<ArmorPointBlankComponent> ent, ref ArmorExamineEvent args)
    {
        if (ent.Comp.Range <= 0f)
            return;

        args.Msg.PushNewline();
        args.Msg.AddMarkupOrThrow(Loc.GetString("armor-point-blank-examine", ("penalty", ent.Comp.ResistancePenalty), ("range", ent.Comp.Range)));
    }
}
