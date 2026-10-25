using Content.Shared.Power;
using Robust.Shared.Prototypes;

using Content.Shared._Trauma.Common.Body;
using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Trauma.Augments;

public sealed partial class AugmentPowerCellSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private AugmentSystem _augment = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<PowerCellDrawComponent> _cellDrawQuery;
    [Dependency] private EntityQuery<AugmentPowerDrawComponent> _drawQuery;

    private TimeSpan _nextUpdate = TimeSpan.Zero;
    private static readonly TimeSpan UpdateDelay = TimeSpan.FromSeconds(2);

    public static readonly ProtoId<OrganCategoryPrototype> SlotCategory = "AugmentPowerCell";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // don't need to burn server tps on alerts
        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;

        _nextUpdate = now + UpdateDelay;

        var query = EntityQueryEnumerator<HasAugmentPowerCellSlotComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (_mob.IsDead(uid) || GetBodyAugment(uid) is not { } augment)
                continue;

            if (!_powerCell.TryGetBatteryFromSlot(augment.Owner, out var battery))
            {
                _alerts.ClearAlert(uid, augment.Comp.BatteryAlert);
                _alerts.ShowAlert(uid, augment.Comp.NoBatteryAlert);
                continue;
            }

            _alerts.ClearAlert(uid, augment.Comp.NoBatteryAlert);

            var batt = battery.Value;
            // number from 0-10... it works
            var chargePercent = (short) _battery.GetRemainingUses(batt.Owner, batt.Comp.MaxCharge * 0.1f);
            _alerts.ShowAlert(uid, augment.Comp.BatteryAlert, chargePercent);
        }
    }

    [SubscribeLocalEvent]
    private void OnOrganEnabled(Entity<AugmentPowerCellSlotComponent> ent, ref OrganEnabledEvent args)
    {
        if (!_cellDrawQuery.TryComp(ent, out var draw))
            return;

        var drawEnt = (ent.Owner, draw);
        UpdateDrawRate(drawEnt, args.Body);

        _powerCell.SetDrawEnabled(drawEnt, true);
        if (!_powerCell.HasDrawCharge(drawEnt))
            return;

        foreach (var augment in _augment.GetAugments(args.Body))
        {
            _body.EnableOrgan(augment);
        }
    }

    [SubscribeLocalEvent]
    private void OnOrganDisabled(Entity<AugmentPowerCellSlotComponent> ent, ref OrganDisabledEvent args)
    {
        _alerts.ClearAlert(args.Body, ent.Comp.BatteryAlert);
        _alerts.ClearAlert(args.Body, ent.Comp.NoBatteryAlert);

        if (!_cellDrawQuery.TryComp(ent, out var draw))
            return;

        var drawEnt = (ent.Owner, draw);
        UpdateDrawRate(drawEnt, args.Body);

        _powerCell.SetDrawEnabled(drawEnt, false);
        // disable every augment that needs power
        foreach (var augment in _augment.GetAugments(args.Body))
        {
            if (_drawQuery.HasComp(augment))
                _body.DisableOrgan(augment);
        }
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<AugmentPowerCellSlotComponent> ent, ref OrganGotRemovedEvent args)
    {
        _alerts.ClearAlert(args.Target, ent.Comp.BatteryAlert);
        _alerts.ClearAlert(args.Target, ent.Comp.NoBatteryAlert);
    }

    [SubscribeLocalEvent]
    private void OnCellEmpty(Entity<AugmentPowerCellSlotComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        if (_body.GetBody(ent) is not { } body)
            return;

        foreach (var augment in _augment.GetAugments(body))
        {
            if (_drawQuery.HasComp(augment))
                _body.DisableOrgan(augment);
        }

        // stop drawing if it loses power
        UpdateDrawRate(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnCellChanged(Entity<AugmentPowerCellSlotComponent> ent, ref PowerCellChangedEvent args)
    {
        if (!args.Ejected)
            RestorePoweredAugments(ent);
    }

    [SubscribeLocalEvent]
    private void OnCellCharged(Entity<AugmentPowerCellSlotComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (args.NewState != BatteryState.Empty)
            RestorePoweredAugments(ent);
    }

    private void RestorePoweredAugments(Entity<AugmentPowerCellSlotComponent> ent)
    {
        if (!HasComp<EnabledOrganComponent>(ent.Owner)
            || _body.GetBody(ent.Owner) is not {} body
            || !_powerCell.HasDrawCharge(ent.Owner))
            return;

        foreach (var augment in _augment.GetAugments(body))
        {
            if (_drawQuery.HasComp(augment))
                _body.EnableOrgan(augment);
        }
    }

    [SubscribeLocalEvent]
    private void OnFindBattery(Entity<HasAugmentPowerCellSlotComponent> ent, ref FindBatteryEvent args)
    {
        args.FoundBattery ??= GetBodyCell(ent);
    }

    [SubscribeLocalEvent]
    private void OnBatteryAlert(Entity<HasAugmentPowerCellSlotComponent> ent, ref AugmentBatteryAlertEvent args)
    {
        var user = args.User;
        if (GetBodyAugment(ent) is not { } augment || !_powerCell.TryGetBatteryFromSlot(augment.Owner, out var battery))
        {
            _popup.PopupEntity(Loc.GetString("power-cell-no-battery"), user, user, PopupType.MediumCaution);
            return;
        }

        var batt = battery.Value;
        var percent = _battery.GetRemainingUses(batt.Owner, batt.Comp.MaxCharge * 0.01f);
        var draw = _cellDrawQuery.CompOrNull(augment)?.DrawRate ?? 0f;
        _popup.PopupEntity(Loc.GetString("augments-power-cell-info", ("percent", percent), ("draw", draw)), user, user);
    }

    public float GetBodyDraw(EntityUid body)
    {
        var ev = new GetAugmentsPowerDrawEvent(body);
        _augment.RelayEvent(body, ref ev);
        return ev.TotalDraw;
    }

    /// <summary>
    /// Update the draw rate for a power cell slot augment.
    /// </summary>
    public void UpdateDrawRate(Entity<PowerCellDrawComponent?> ent, EntityUid? bodyUid = null)
    {
        if (!_cellDrawQuery.Resolve(ent, ref ent.Comp))
            return;

        bodyUid ??= _body.GetBody(ent.Owner);
        var rate = bodyUid is { } body
            ? GetBodyDraw(body)
            : 0f;
        if (ent.Comp.DrawRate == rate)
            return;

        _powerCell.SetDrawRate(ent, rate);
    }

    /// <summary>
    /// Get a body's power cell slot augment, or null if it has none.
    /// </summary>
    public Entity<AugmentPowerCellSlotComponent>? GetBodyAugment(EntityUid body)
        => _body.GetOrgan(body, SlotCategory) is { } organ && TryComp<AugmentPowerCellSlotComponent>(organ, out var slot)
            ? (organ, slot)
            : null;

    /// <summary>
    /// Gets a power cell for a body if it both:
    /// 1. has a power cell slot augment
    /// 2. that augment has a power cell installed
    /// Returns null otherwise.
    /// </summary>
    public Entity<BatteryComponent>? GetBodyCell(EntityUid body)
        => GetBodyAugment(body) is { } augment && _powerCell.TryGetBatteryFromSlot(augment.Owner, out var battery)
            ? battery
            : null;

    /// <summary>
    /// Tries to use charge from a body's power cell slot augment.
    /// Does a popup for the user if it fails.
    /// </summary>
    public bool TryUseChargeBody(EntityUid body, float amount)
    {
        if (GetBodyAugment(body) is not { } slot)
        {
            _popup.PopupEntity(Loc.GetString("augments-no-power-cell-slot"), body, body, PopupType.MediumCaution);
            return false;
        }

        if (!_powerCell.TryGetBatteryFromSlot(slot.Owner, out var battery))
        {
            _popup.PopupEntity(Loc.GetString("power-cell-no-battery"), body, body, PopupType.MediumCaution);
            return false;
        }

        if (!_battery.TryUseCharge(battery.Value.AsNullable(), amount))
        {
            _popup.PopupEntity(Loc.GetString("power-cell-insufficient"), body, body, PopupType.MediumCaution);
            return false;
        }

        return true;
    }
}
