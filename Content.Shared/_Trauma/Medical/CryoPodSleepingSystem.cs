using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Medical.Cryogenics;

namespace Content.Shared._Trauma.Medical;

/// <summary>
/// Lets you sleep inside cryo pods.
/// Being ejected from a cryo pod automatically tries to wake you up.
/// </summary>
public sealed partial class CryoPodSleepingSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SleepingSystem _sleeping = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<InsideCryoPodComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.SleepAction, SleepingSystem.SleepActionId);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<InsideCryoPodComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.SleepAction);
        _sleeping.TryWaking(ent.Owner);
    }
}
