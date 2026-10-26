using Content.Shared._Trauma.DelayedDeath;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Trauma.Body;

// TODO: Share brain and heart handling in a generic vital-organ system.
public sealed partial class HeartSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IGameTiming _timing = default!;

    public static readonly ProtoId<OrganCategoryPrototype> Brain = "Brain";

    [SubscribeLocalEvent]
    private void HandleRemoval(EntityUid uid, HeartComponent _, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target) || _timing.ApplyingState)
            return;

        // TODO: Add some form of very violent bleeding effect.
        EnsureComp<DelayedDeathComponent>(args.Target);
    }

    [SubscribeLocalEvent]
    private void HandleAddition(EntityUid uid, HeartComponent _, ref OrganGotInsertedEvent args)
    {
        if (_timing.ApplyingState || TerminatingOrDeleted(uid) || TerminatingOrDeleted(args.Target))
            return;

        if (_body.GetOrgan(args.Target, Brain) != null)
            RemComp<DelayedDeathComponent>(args.Target);
    }
}
