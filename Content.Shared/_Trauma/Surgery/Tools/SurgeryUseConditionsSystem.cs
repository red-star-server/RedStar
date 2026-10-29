using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.IgnitionSource.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Smoking;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._Trauma.Surgery.Tools;

/// <summary>
/// Validates stateful items before they are used during a surgery step.
/// </summary>
public sealed partial class SurgeryUseConditionsSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnToggleUsed(Entity<ItemToggleComponent> ent, ref SurgeryUseAttemptEvent args)
    {
        // Augment toggles represent implant state, not whether the tool can be used.
        if (ent.Comp.Activated || !ent.Comp.OnUse || args.IgnoreToggle)
            return;

        _popup.PopupEntity(Loc.GetString("surgery-tool-turn-on"), ent, args.User);
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnGunUsed(Entity<GunComponent> ent, ref SurgeryUseAttemptEvent args)
    {
        var coords = Transform(args.User).Coordinates;
        var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(), coords, args.User);
        RaiseLocalEvent(ent, ev);

        if (ev.Ammo.Count > 0)
            return;

        _popup.PopupEntity(Loc.GetString("surgery-tool-reload"), ent, args.User);
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnMatchUsed(Entity<MatchstickComponent> ent, ref SurgeryUseAttemptEvent args)
        => SmokableUsed(ent, ent.Comp.State, ref args);

    [SubscribeLocalEvent]
    private void OnSmokableUsed(Entity<SmokableComponent> ent, ref SurgeryUseAttemptEvent args)
        => SmokableUsed(ent, ent.Comp.State, ref args);

    private void SmokableUsed(EntityUid uid, SmokableState state, ref SurgeryUseAttemptEvent args)
    {
        if (state == SmokableState.Lit)
            return;

        var key = "surgery-tool-match-" + (state == SmokableState.Burnt ? "replace" : "light");
        _popup.PopupEntity(Loc.GetString(key), uid, args.User);
        args.Cancelled = true;
    }
}

/// <summary>
/// Raised before a surgery step to validate stateful items such as toggles and guns.
/// </summary>
[ByRefEvent]
public record struct SurgeryUseAttemptEvent(EntityUid User, EntityUid Target, bool IgnoreToggle = false, bool Cancelled = false);
