using System.Linq;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared.Body.Events;
using Content.Shared.Metabolism;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared._RedStar.Genetics.Systems;

public sealed partial class GeneMetabolismSystem : EntitySystem
{
    [Dependency] private MetabolizerSystem _metabolism = default!;

    private readonly HashSet<EntityUid> _pendingRefresh = [];

    [SubscribeLocalEvent]
    private void OnApplied(Entity<GeneMetabolismComponent> ent, ref StatusEffectAppliedEvent args) =>
        _pendingRefresh.Add(args.Target);

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<GeneMetabolismComponent> ent, ref StatusEffectRemovedEvent args) =>
        _pendingRefresh.Add(args.Target);

    [SubscribeLocalEvent]
    private void OnMetabolicMultiplier(Entity<StatusEffectContainerComponent> ent, ref GetMetabolicMultiplierEvent args) =>
        args.Multiplier *= GetMultiplier(ent);

    private float GetMultiplier(Entity<StatusEffectContainerComponent> ent)
    {
        var multiplier = 1f;
        var minimum = 0f;
        var maximum = float.PositiveInfinity;
        foreach (var uid in ent.Comp.ActiveStatusEffects?.ContainedEntities ?? [])
        {
            if (!TryComp<GeneMetabolismComponent>(uid, out var metabolism) ||
                !TryComp<StatusEffectComponent>(uid, out var status) || !status.Applied || TerminatingOrDeleted(uid))
                continue;

            multiplier += metabolism.IntervalModifier;
            minimum = MathF.Max(minimum, metabolism.MinimumMultiplier);
            maximum = MathF.Min(maximum, metabolism.MaximumMultiplier);
        }
        return Math.Clamp(multiplier, minimum, MathF.Max(minimum, maximum));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_pendingRefresh.Count == 0)
            return;

        var pending = _pendingRefresh.ToArray();
        _pendingRefresh.Clear();
        foreach (var uid in pending)
        {
            if (TerminatingOrDeleted(uid))
                continue;

            _metabolism.UpdateMetabolicMultiplier(uid);
        }
    }
}
