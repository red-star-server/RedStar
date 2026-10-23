using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Robust.Shared.Timing;

namespace Content.Shared._Trauma.Body;

public sealed partial class OrganComponentsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnInserted(Entity<OrganComponentsComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        // TODO SHITMED: this doesn't actually have any kind of refcounting :)
        if (ent.Comp.OnAdd is {} adding)
            EntityManager.AddComponents(args.Target, adding);
        if (ent.Comp.OnRemove is {} removing)
            EntityManager.RemoveComponents(args.Target, removing);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<OrganComponentsComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (_timing.ApplyingState || TerminatingOrDeleted(args.Target))
            return;

        if (ent.Comp.OnRemove is {} removed)
            EntityManager.AddComponents(args.Target, removed);
        if (ent.Comp.OnAdd is {} added)
            EntityManager.RemoveComponents(args.Target, added);
    }
}
