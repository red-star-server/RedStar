using Content.Shared.Body;
using Content.Shared.Interaction;

namespace Content.Shared._Trauma.Augments;

public sealed partial class AugmentSystem : EntitySystem
{
    [Dependency] private AugmentPowerCellSystem _power = default!;
    [Dependency] private EntityQuery<InstalledAugmentsComponent> _installedQuery = default!;
    [Dependency] private EntityQuery<OrganComponent> _organQuery;

    [SubscribeLocalEvent]
    private void OnAdded(Entity<AugmentComponent> augment, ref OrganGotInsertedEvent args)
    {
        var installed = EnsureComp<InstalledAugmentsComponent>(args.Target);
        installed.InstalledAugments.Add(augment);
        UpdatePowerDraw(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<AugmentComponent> augment, ref OrganGotRemovedEvent args)
    {
        if (!TryComp<InstalledAugmentsComponent>(args.Target, out var installed))
            return;

        installed.InstalledAugments.Remove(augment);
        UpdatePowerDraw(args.Target);
        if (installed.InstalledAugments.Count == 0)
            RemCompDeferred(args.Target, installed);
    }

    [SubscribeLocalEvent]
    private void OnAccessibleOverride(Entity<InstalledAugmentsComponent> ent, ref AccessibleOverrideEvent args)
    {
        if (!HasComp<AugmentComponent>(args.Target) || GetBody(args.Target) is not {} body || body != args.User)
            return;

        // let the user interact with their installed augments
        args.Handled = true;
        args.Accessible = true;
    }

    private void UpdatePowerDraw(EntityUid body)
    {
        if (_power.GetBodyAugment(body) is {} slot)
            _power.UpdateDrawRate(slot.Owner, body);
    }

    #region Public API

    /// <summary>
    /// Get the body linked to an augment's organ.
    /// Returns null if not installed into a body.
    /// </summary>
    public EntityUid? GetBody(EntityUid uid) => _organQuery.CompOrNull(uid)?.Body;

    /// <summary>
    /// Relays an event to all installed augments.
    /// </summary>
    public void RelayEvent<T>(EntityUid body, ref T ev) where T: notnull
    {
        if (_installedQuery.TryComp(body, out var comp))
            RelayEvent((body, comp), ref ev);
    }

    /// <summary>
    /// Relay an event in the form usable for a subscription.
    /// </summary>
    public void RelayEvent<T>(Entity<InstalledAugmentsComponent> ent, ref T ev) where T: notnull
    {
        foreach (var aug in ent.Comp.InstalledAugments)
        {
            RaiseLocalEvent(aug, ref ev);
        }
    }

    /// <summary>
    /// Gets the installed augments for a mob, or an empty one if it has no augments.
    /// </summary>
    public HashSet<EntityUid> GetAugments(Entity<InstalledAugmentsComponent?> ent)
        => _installedQuery.Resolve(ent, ref ent.Comp, false)
            ? ent.Comp.InstalledAugments
            : new();

    #endregion
}
