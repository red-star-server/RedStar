using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Traumas;
using Content.Shared._Trauma.Wounds;
using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Shared._Trauma.Body;

public sealed partial class BodyBloodstreamSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private WoundSystem _wound = default!;
    [Dependency] private EntityQuery<BleedInflicterComponent> _bleedQuery;
    [Dependency] private EntityQuery<WoundableComponent> _woundableQuery;

    private float _bleedingSeverity = 1f;

    /// <summary>
    /// Add a bleed-ability modifier on woundable
    /// </summary>
    /// <param name="part">The bodypart to apply the modifiers</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <param name="priority">Priority of the said modifier</param>
    /// <param name="canBleed">Should the wounds bleed?</param>
    /// <param name="force">If forced, won't stop after failing to apply one modifier</param>
    /// <returns>Return true if applied</returns>
    public bool TryAddPartBleedModifier(
        Entity<WoundableComponent?> part,
        string identifier,
        int priority,
        bool canBleed,
        bool force = false)
    {
        foreach (var wound in _wound.GetWoundableWounds(part))
        {
            if (!_bleedQuery.TryComp(wound, out var bleeds))
                continue;

            if (TryAddBleedModifier((wound, bleeds), identifier, priority, canBleed))
                continue;

            if (!force)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Add a bleed-ability modifier
    /// </summary>
    /// <param name="ent">The wound</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <param name="priority">Priority of the said modifier</param>
    /// <param name="canBleed">Should the wound bleed?</param>
    /// <returns>Return true if applied</returns>
    public bool TryAddBleedModifier(
        Entity<BleedInflicterComponent?> ent,
        string identifier,
        int priority,
        bool canBleed)
    {
        if (!_bleedQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.BleedingModifiers.TryAdd(identifier, (priority, canBleed)))
            return false;

        DirtyField(ent, ent.Comp, nameof(BleedInflicterComponent.BleedingModifiers));
        return true;
    }

    /// <summary>
    /// Remove a bleed-ability modifier from a woundable
    /// </summary>
    /// <param name="part">The bodypart</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <param name="force">If forced, won't stop applying modifiers after failing one wound</param>
    /// <returns>Returns true if removed all modifiers ON WOUNDABLE</returns>
    public bool TryRemoveBleedModifier(
        Entity<WoundableComponent?> part,
        string identifier,
        bool force = false)
    {
        foreach (var wound in _wound.GetWoundableWounds(part))
        {
            if (!_bleedQuery.TryComp(wound, out var bleeds))
                continue;

            if (TryRemoveBleedModifier((wound, bleeds), identifier))
                continue;

            if (!force)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Remove a bleed-ability modifier
    /// </summary>
    /// <param name="ent">The wound</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <returns>Return true if removed</returns>
    public bool TryRemoveBleedModifier(
        Entity<BleedInflicterComponent?> ent,
        string identifier)
    {
        if (!_bleedQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.BleedingModifiers.Remove(identifier))
            return false;

        DirtyField(ent, ent.Comp, nameof(BleedInflicterComponent.BleedingModifiers));
        return true;
    }

    /// <summary>
    /// Self-explanatory
    /// </summary>
    /// <returns>Returns whether if the wound can bleed</returns>
    public bool CanWoundBleed(Entity<BleedInflicterComponent?> ent)
    {
        if (!_bleedQuery.Resolve(ent, ref ent.Comp))
            return false;

        var nearestModifier = ent.Comp.BleedingModifiers.FirstOrNull();
        if (nearestModifier == null)
            return true;

        var lastCanBleed = true;
        var lastPriority = int.MinValue;
        foreach (var (_, pair) in ent.Comp.BleedingModifiers)
        {
            if (pair.Priority <= lastPriority)
                continue;

            lastPriority = pair.Priority;
            lastCanBleed = pair.CanBleed;
        }

        return lastCanBleed;
    }

    [SubscribeLocalEvent]
    private void OnWoundAdded(EntityUid uid, BleedInflicterComponent component, ref WoundAddedEvent args)
    {
        if (!args.Woundable.CanBleed ||
            !CanWoundBleed((uid, component)) ||
            args.Component.WoundSeverityPoint < component.SeverityThreshold)
            return;

        component.BleedingAmount = args.Component.WoundSeverityPoint * _bleedingSeverity;
        component.IsBleeding = true;

        Dirty(uid, component);

        if (_body.GetBody(args.Component.HoldingWoundable) is not { } body || !HasComp<BloodstreamComponent>(body)) return;
        var update = new BloodstreamUpdateEvent();
        RaiseLocalEvent(body, ref update);
    }

    [SubscribeLocalEvent]
    private void OnWoundHealAttempt(EntityUid uid, BleedInflicterComponent component, ref WoundHealAttemptEvent args)
    {
        if (args.IgnoreBlockers)
            return;

        if (component.IsBleeding)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnBleedInflicterSeverityUpdate(EntityUid uid,
        BleedInflicterComponent component,
        ref WoundSeverityPointChangedEvent args)
    {
        if (!CanWoundBleed((uid, component))
            || !_woundableQuery.TryComp(args.Component.HoldingWoundable, out var woundable)
            || !woundable.CanBleed
            || args.NewSeverity < component.SeverityThreshold
            || args.NewSeverity < args.OldSeverity)
            return;

        component.BleedingAmount = args.NewSeverity * _bleedingSeverity;
        component.IsBleeding = true;

        Dirty(uid, component);
    }

    [SubscribeLocalEvent]
    public void OnBleedRemoverSeverityUpdate(Entity<BleedRemoverComponent> ent, ref WoundSeverityPointChangedEvent args)
    {
        var delta = args.NewSeverity - args.OldSeverity;
        var part = args.Component.HoldingWoundable;
        if (delta < ent.Comp.SeverityThreshold ||
            TerminatingOrDeleted(part) ||
            _body.GetBody(part) is not {} body)
            return;

        var result = _wound.TryHealBleedingWounds(part,
            delta * ent.Comp.BleedingRemovalMultiplier,
            out _);

        if (!result)
            return;

        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/lightburn.ogg"), body, body);
        _popup.PopupEntity(Loc.GetString("bloodstream-component-wounds-cauterized"),
            body,
            body,
            PopupType.MediumCaution);
    }

    [SubscribeLocalEvent]
    private void OnBodyUpdate(Entity<BodyComponent> ent, ref BloodstreamUpdateEvent args)
    {
        var total = FixedPoint2.Zero;
        foreach (var part in _body.GetOrgans<WoundableComponent>(ent.AsNullable()))
        {
            var totalPartBleeds = FixedPoint2.Zero;
            foreach (var wound in _wound.GetWoundableWounds(part.AsNullable()))
            {
                if (_bleedQuery.TryComp(wound, out var bleeds) && bleeds.IsBleeding)
                    totalPartBleeds += bleeds.BleedingAmount;
            }
            total += totalPartBleeds;

            if (part.Comp.Bleeds == totalPartBleeds)
                continue;

            part.Comp.Bleeds = totalPartBleeds;
            DirtyField(part, part.Comp, nameof(WoundableComponent.Bleeds));
        }

        var blood = Comp<BloodstreamComponent>(ent);
        blood.BleedAmountFromWounds = (float) total;
        blood.BleedAmount = blood.BleedAmountFromWounds + blood.BleedAmountNotFromWounds;
        blood.BleedAmount = Math.Clamp(blood.BleedAmount, 0, blood.MaxBleedAmount);
        DirtyFields(ent.Owner, blood, null, nameof(BloodstreamComponent.BleedAmount), nameof(BloodstreamComponent.BleedAmountFromWounds));

        if (blood.BleedAmount == 0)
        {
            _alerts.ClearAlert(ent.Owner, blood.BleedingAlert);
        }
        else
        {
            var severity = (short) Math.Clamp(Math.Round(blood.BleedAmount, MidpointRounding.ToZero), 0, 10);
            _alerts.ShowAlert(ent.Owner, blood.BleedingAlert, severity);
        }
    }
}
