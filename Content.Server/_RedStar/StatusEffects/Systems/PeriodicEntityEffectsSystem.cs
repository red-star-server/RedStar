using Content.Server._RedStar.StatusEffects.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.StatusEffects.Systems;

public sealed partial class PeriodicEntityEffectsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnApplied(Entity<PeriodicEntityEffectsComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ent.Comp.Interval, TimeSpan.Zero);
        ent.Comp.NextEffect = _timing.CurTime + ent.Comp.Interval;
    }

    [SubscribeLocalEvent]
    private void OnMoved(Entity<StatusEffectContainerComponent> ent, ref MoveEvent args)
    {
        if (args.OldPosition == args.NewPosition)
            return;

        foreach (var uid in ent.Comp.ActiveStatusEffects?.ContainedEntities ?? [])
        {
            if (!TryComp<PeriodicEntityEffectsComponent>(uid, out var periodic) || periodic.DelayAfterMovement <= TimeSpan.Zero)
                continue;

            var next = _timing.CurTime + periodic.DelayAfterMovement;
            if (next > periodic.NextEffect)
                periodic.NextEffect = next;
        }
    }

    [SubscribeLocalEvent]
    private void OnDamage(Entity<StatusEffectContainerComponent> ent, ref DamageDealtEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        foreach (var uid in ent.Comp.ActiveStatusEffects?.ContainedEntities ?? [])
        {
            if (!TryComp<PeriodicEntityEffectsComponent>(uid, out var periodic) || periodic.DelayAfterDamage <= TimeSpan.Zero)
                continue;

            var next = _timing.CurTime + periodic.DelayAfterDamage;
            if (next > periodic.NextEffect)
                periodic.NextEffect = next;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<PeriodicEntityEffectsComponent, StatusEffectComponent>();
        while (query.MoveNext(out var uid, out var periodic, out var status))
        {
            if (!status.Applied || status.AppliedTo is not { } target || TerminatingOrDeleted(target) ||
                MetaData(uid).EntityPaused || MetaData(target).EntityPaused || now < periodic.NextEffect ||
                status.EndEffectTime is { } end && now >= end)
                continue;

            // Do not replay missed pulses after a pause or a long frame.
            periodic.NextEffect = now + periodic.Interval;
            if (_conditions.TryConditions(target, periodic.Conditions, uid))
                _effects.ApplyEffects(target, periodic.Effects);
        }
    }
}
