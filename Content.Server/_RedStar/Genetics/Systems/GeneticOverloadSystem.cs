using Content.Server._RedStar.Genetics.Components;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared._RedStar.Genetics.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Genetics.Systems;

/// <summary>
/// Applies configured symptoms and damage while genetic load exceeds safe capacity.
/// Damage uses the current load on each pulse, so lowering load takes effect immediately.
/// </summary>
public sealed partial class GeneticOverloadSystem : EntitySystem
{
    [Dependency] private ServerGenomeSystem _genomes = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnGenomeChanged(ref GenomeChangedEvent args)
    {
        var uid = args.Subject;
        if (TerminatingOrDeleted(uid))
            return;

        if (!TryComp<GenomeComponent>(uid, out var genome) || !genome.Running || _genomes.GetLoadPercentage(uid) <= 100)
        {
            ClearOverload(uid);
            return;
        }

        var overload = EnsureComp<GeneticOverloadComponent>(uid);
        if (!overload.Active)
        {
            overload.Active = true;
            overload.NextPulse = _timing.CurTime + overload.PulseInterval;
            _popup.PopupEntity(Loc.GetString("genetic-overload-weakness"), uid, uid);
        }

        _status.TrySetStatusEffectDuration(uid, overload.Weakness);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GeneticOverloadComponent> ent, ref ComponentShutdown args) =>
        ClearOverload(ent.AsNullable());

    private void ClearOverload(Entity<GeneticOverloadComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
            return;
        ent.Comp.Active = false;
        ent.Comp.NextPulse = TimeSpan.Zero;
        if (_status.TryGetStatusEffect(ent, ent.Comp.Weakness, out var effect))
            Del(effect.Value);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<GeneticOverloadComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var overload, out var metadata))
        {
            if (!overload.Active || metadata.EntityPaused || overload.NextPulse > _timing.CurTime)
                continue;

            overload.NextPulse = _timing.CurTime + overload.PulseInterval;
            var percentage = _genomes.GetLoadPercentage(uid);
            if (percentage <= 100)
            {
                ClearOverload(uid);
                continue;
            }
            _status.TrySetStatusEffectDuration(uid, overload.Weakness);
            if (percentage <= overload.DamageThreshold)
                continue;

            var mildRange = MathF.Max(overload.SevereDamageThreshold - overload.DamageThreshold, 1);
            var scale = percentage <= overload.SevereDamageThreshold
                ? (percentage - overload.DamageThreshold) / mildRange
                : 1 + MathF.Pow(percentage - overload.SevereDamageThreshold, 2) * overload.SevereDamageScale;
            var damage = overload.Damage * scale;
            _damage.TryChangeDamage(uid, damage, ignoreResistances: overload.IgnoreResistances,
                interruptsDoAfters: overload.InterruptsDoAfters);
        }
    }
}
