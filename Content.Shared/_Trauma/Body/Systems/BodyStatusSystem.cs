using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.Mobs;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Body;

public sealed partial class BodyStatusSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
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

    public void UpdateStatus(Entity<BodyStatusComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.BodyStatus = _wound.GetWoundableStatesOnBody(ent.Owner);
        Dirty(ent, ent.Comp);

        var ev = new TargetIntegrityChangedMessage();
        if (_net.IsClient)
            RaiseLocalEvent(ev);
        else
            RaiseNetworkEvent(ev, ent);
    }
}

/// <summary>
/// Message sent by the server/predicted when a body's parts get damaged, to update the part status UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class TargetIntegrityChangedMessage: EntityEventArgs;
