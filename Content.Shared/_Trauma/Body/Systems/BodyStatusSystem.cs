using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.Mobs;

namespace Content.Shared._Trauma.Body;

public sealed partial class BodyStatusSystem : EntitySystem
{
    [Dependency] private WoundSystem _wound = default!;


    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BodyStatusComponent> ent, ref MapInitEvent args)
    {
        UpdateStatus(ent.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnMobStateChange(Entity<BodyStatusComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateStatus(ent.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnOrganInserted(Entity<BodyStatusComponent> ent, ref OrganInsertedIntoEvent args)
    {
        UpdateStatus(ent.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnOrganRemoved(Entity<BodyStatusComponent> ent, ref OrganRemovedFromEvent args)
    {
        UpdateStatus(ent.AsNullable());
    }

    /// <summary>
    /// Recomputes the body status information used by medical HUDs.
    /// </summary>
    public void UpdateStatus(Entity<BodyStatusComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.BodyStatus = _wound.GetWoundableStatesOnBody(ent.Owner);
        Dirty(ent, ent.Comp);

        var ev = new BodyStatusChangedEvent();
        RaiseLocalEvent(ent, ref ev);
    }
}

/// <summary>
/// Raised after the body status is recomputed, including during prediction.
/// </summary>
[ByRefEvent]
public readonly record struct BodyStatusChangedEvent;
