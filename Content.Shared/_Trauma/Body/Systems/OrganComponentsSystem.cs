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
        if (_timing.ApplyingState || ent.Comp.OnlyWhileEnabled)
            return;

        ApplyComponents(ent, args.Target);
    }

    [SubscribeLocalEvent]
    private void OnEnabled(Entity<OrganComponentsComponent> ent, ref OrganEnabledEvent args)
    {
        if (!_timing.ApplyingState && ent.Comp.OnlyWhileEnabled)
            ApplyComponents(ent, args.Body);
    }

    private void ApplyComponents(Entity<OrganComponentsComponent> ent, EntityUid body)
    {
        // TODO SHITMED: this doesn't actually have any kind of refcounting :)
        if (ent.Comp.OnAdd is {} adding)
            EntityManager.AddComponents(body, adding);
        if (ent.Comp.OnRemove is {} removing)
            EntityManager.RemoveComponents(body, removing);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<OrganComponentsComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (_timing.ApplyingState || ent.Comp.OnlyWhileEnabled || TerminatingOrDeleted(args.Target))
            return;

        RemoveComponents(ent, args.Target);
    }

    [SubscribeLocalEvent]
    private void OnDisabled(Entity<OrganComponentsComponent> ent, ref OrganDisabledEvent args)
    {
        if (!_timing.ApplyingState && ent.Comp.OnlyWhileEnabled && !TerminatingOrDeleted(args.Body))
            RemoveComponents(ent, args.Body);
    }

    private void RemoveComponents(Entity<OrganComponentsComponent> ent, EntityUid body)
    {
        if (ent.Comp.OnRemove is {} removed)
            EntityManager.AddComponents(body, removed);
        if (ent.Comp.OnAdd is {} added)
            EntityManager.RemoveComponents(body, added);
    }
}
