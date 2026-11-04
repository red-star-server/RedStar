using Content.Shared._Trauma.Common.Body;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared._Trauma.Body.Cavity;

public sealed partial class BodyPartCavitySystem : EntitySystem
{
    [Dependency] private CommonInsideBodyPartSystem _insideBodyPart = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BodyPartCavityComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.ContainerId);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<BodyPartCavityComponent> ent, ref ComponentShutdown args)
    {
        if (_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
            _container.ShutdownContainer(container);
    }

    [SubscribeLocalEvent]
    private void OnGetBodyPartCavity(Entity<BodyPartCavityComponent> ent, ref GetBodyPartCavityEvent args)
    {
        if (args.Container != null)
            return;

        if (_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container) && container is ContainerSlot slot)
            args.Container = slot;
    }

    [SubscribeLocalEvent]
    private void OnInsertingAttempt(Entity<BodyPartCavityComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (!_whitelist.CheckBoth(args.EntityUid, blacklist: ent.Comp.Blacklist, whitelist: ent.Comp.Whitelist))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<BodyPartCavityComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        // this will give a mob inserted into someones chest an action to burst out
        _insideBodyPart.InsertedIntoPart(args.Entity, ent);
        var ev = new InsertedIntoCavityEvent(ent);
        RaiseLocalEvent(args.Entity, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<BodyPartCavityComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        // this will remove the chest burst action
        _insideBodyPart.RemovedFromPart(args.Entity);
        var ev = new RemovedFromCavityEvent(ent);
        RaiseLocalEvent(args.Entity, ref ev);
    }

    /// <summary>
    /// Returns true if a bodypart has a item in its cavity.
    /// </summary>
    public bool HasItem(Entity<BodyPartCavityComponent> ent)
        => _container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container) &&
            container.Count > 0;
}

/// <summary>
/// Raised on the item after it is inserted into a body part cavity.
/// </summary>
[ByRefEvent]
public record struct InsertedIntoCavityEvent(EntityUid Part);

/// <summary>
/// Raised on the item after it is removed from a body part cavity.
/// </summary>
[ByRefEvent]
public record struct RemovedFromCavityEvent(EntityUid Part);
