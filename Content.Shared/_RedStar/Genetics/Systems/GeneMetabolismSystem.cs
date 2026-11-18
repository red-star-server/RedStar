using System.Linq;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared.Body.Events;
using Content.Shared.Metabolism;
using Content.Shared.StatusEffectNew;

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
    private void OnMetabolicMultiplier(Entity<GeneMetabolismComponent> ent, ref StatusEffectRelayedEvent<GetMetabolicMultiplierEvent> args)
    {
        var multiplier = Math.Clamp(1f + ent.Comp.IntervalModifier, ent.Comp.MinimumMultiplier, ent.Comp.MaximumMultiplier);
        var arg = args.Args;
        arg.Multiply(multiplier);
        args.Args = arg;
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
