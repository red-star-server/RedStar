using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public sealed partial class BrainSystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IGameTiming _timing = default!;

    // Use the active-organ logic and preserve an existing brain.
    [SubscribeLocalEvent]
    private void OnRemoved(EntityUid uid, BrainComponent brain, ref OrganGotRemovedEvent args)
    {
        var attemptEv = new BeforeBrainRemovedEvent();
        RaiseLocalEvent(args.Target, ref attemptEv);

        if (attemptEv.Blocked)
            return;

        brain.Active = false;
        Dirty(uid, brain);
        if (HasBrain(args.Target)) return;
        // Prevents revival, should kill the user within a given timespan too.
        if (!TerminatingOrDeleted(args.Target) && !_timing.ApplyingState)
            EnsureComp<DebrainedComponent>(args.Target);
        HandleMind(uid, args.Target);
    }

    [SubscribeLocalEvent]
    private void OnAdded(EntityUid uid, BrainComponent brain, ref OrganGotInsertedEvent args)
    {
        var attemptEv = new BeforeBrainAddedEvent();
        RaiseLocalEvent(args.Target, ref attemptEv);

        if (attemptEv.Blocked)
            return;

        brain.Active = true;
        Dirty(uid, brain);
        if (!HasBrain(args.Target)) return;
        RemComp<DebrainedComponent>(args.Target);
        HandleMind(args.Target, uid);
    }

    private bool HasBrain(EntityUid entity)
    {
        if (!TryComp<BodyComponent>(entity, out var body))
            return false;

        if (HasComp<BrainComponent>(entity))
            return true;

        foreach (var brain in _body.GetOrgans<BrainComponent>((entity, body)))
        {
            if (brain.Comp.Active)
                return true;
        }

        return false;
    }
}
