using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public sealed partial class BrainSystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IGameTiming _timing = default!;

    // <Shitmed> - do nothing for lings, use Active logic and don't do anything if a body already has a brain
    [SubscribeLocalEvent]
    private void OnRemoved(EntityUid uid, BrainComponent brain, ref OrganGotRemovedEvent args)
    {
        // <Goob>
        var attemptEv = new BeforeBrainRemovedEvent();
        RaiseLocalEvent(args.Target, ref attemptEv);

        if (attemptEv.Blocked)
            return;
        // </Goob>

        brain.Active = false;
        Dirty(uid, brain);
        if (!HasBrain(args.Target))
        {
            // Prevents revival, should kill the user within a given timespan too.
            if (!TerminatingOrDeleted(args.Target) && !_timing.ApplyingState)
                EnsureComp<DebrainedComponent>(args.Target);
            HandleMind(uid, args.Target);
        }
    }

    [SubscribeLocalEvent]
    private void OnAdded(EntityUid uid, BrainComponent brain, ref OrganGotInsertedEvent args)
    {
        // <Goob>
        var attemptEv = new BeforeBrainAddedEvent();
        RaiseLocalEvent(args.Target, ref attemptEv);

        if (attemptEv.Blocked)
            return;
        // </Goob>

        brain.Active = true;
        Dirty(uid, brain);
        if (HasBrain(args.Target))
        {
            RemComp<DebrainedComponent>(args.Target);
            HandleMind(args.Target, uid);
        }
    }

    private bool HasBrain(EntityUid entity)
    {
        if (!TryComp<BodyComponent>(entity, out var body))
            return false;

        if (HasComp<BrainComponent>(entity)) // sentient brain...
            return true;

        foreach (var brain in _body.GetOrgans<BrainComponent>((entity, body)))
        {
            if (brain.Comp.Active)
                return true;
        }

        return false;
    }
}
