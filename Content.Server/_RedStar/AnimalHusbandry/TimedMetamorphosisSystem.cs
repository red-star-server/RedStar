using Content.Shared.Coordinates;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.AnimalHusbandry;

/// <summary>
/// Transfers position, rotation, proportional damage, satiation and mind to the replacement.
/// </summary>
public sealed partial class TimedMetamorphosisSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<TimedMetamorphosisComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.EndTime = _timing.CurTime + ent.Comp.Duration;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<TimedMetamorphosisComponent>();
        while (query.MoveNext(out var uid, out var metamorphosis))
        {
            if (_timing.CurTime < metamorphosis.EndTime || _mobState.IsDead(uid))
                continue;

            var oldTransform = Transform(uid);
            var result = Spawn(metamorphosis.Target, uid.ToCoordinates());
            _transform.SetLocalRotation(result, oldTransform.LocalRotation);

            if (TryComp<SatiationComponent>(uid, out var oldSatiation) &&
                TryComp<SatiationComponent>(result, out var newSatiation))
            {
                foreach (var type in oldSatiation.Satiations.Keys)
                {
                    if (newSatiation.Has(type) &&
                        _satiation.GetValueOrNull((uid, oldSatiation), type) is { } value)
                        _satiation.SetValue((result, newSatiation), type, value);
                }
            }

            if (TryComp<DamageableComponent>(result, out var damageable) &&
                _threshold.GetScaledDamage(uid, result, out var damage) && damage != null)
                _damageable.SetDamage((result, damageable), damage);

            var ev = new TimedMetamorphosisEvent(result);
            RaiseLocalEvent(uid, ref ev);
            if (_mind.TryGetMind(uid, out var mindId, out var mind))
                _mind.TransferTo(mindId, result, mind: mind);

            QueueDel(uid);
        }
    }
}
