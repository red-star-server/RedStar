using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Content.Shared.Gibbing;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared._Trauma.Body;

public sealed partial class UnremoveableOrganSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnRemoveAttempt(Entity<UnremoveableOrganComponent> ent, ref OrganRemoveAttemptEvent args)
    {
        args.Cancelled |= ent.Owner == args.Organ;
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<UnremoveableOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target) || Transform(args.Target).MapID == MapId.Nullspace || _timing.ApplyingState)
            return; // all good if it's being deleted or leaving pvs range

        // if you intentionally deleted the root part, please delete the body instead chud
        if (!HasComp<ChildOrganComponent>(ent))
        {
            Log.Warning($"Root part {ToPrettyString(ent)} was removed from {ToPrettyString(args.Target)}; deleting the body.");
            QueueDel(args.Target);
            return;
        }

        Log.Warning($"{ToPrettyString(ent)} somehow got removed from {ToPrettyString(args.Target)}!");
    }

    [SubscribeLocalEvent]
    private void OnBeingGibbed(Entity<UnremoveableOrganComponent> ent, ref BeingGibbedEvent args)
    {
        // this is specifically if the torso gets gibbed, if the body is gibbed it uses BodyRelayedEvent anyway
        if (HasComp<ChildOrganComponent>(ent) || _body.GetBody(ent.Owner) is not {} body)
            return;

        Log.Info($"Root part {ToPrettyString(ent)} was gibbed, gibbing {ToPrettyString(ent)} too!");
        _gibbing.Gib(body);
    }
}
