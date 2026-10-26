using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Trauma.Mobs;

/// <summary>
/// Manages adding and removing per-state components to mobs.
/// </summary>
public sealed partial class MobStateComponentsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<SleepingComponent> _sleepingQuery = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<MobStateComponent> ent, ref MapInitEvent args)
    {
        AddState(ent, ent.Comp.CurrentState);
    }

    [SubscribeLocalEvent]
    private void OnAliveMobComponentStateChanged(Entity<AliveMobComponent> ent, ref MobStateChangedEvent args)
        => OnStateChanged(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnSoftCritMobComponentStateChanged(Entity<SoftCritMobComponent> ent, ref MobStateChangedEvent args)
        => OnStateChanged(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnCriticalMobComponentStateChanged(Entity<CriticalMobComponent> ent, ref MobStateChangedEvent args)
        => OnStateChanged(ent.Owner, ent.Comp, ref args);

    [SubscribeLocalEvent]
    private void OnDeadMobComponentStateChanged(Entity<DeadMobComponent> ent, ref MobStateChangedEvent args)
        => OnStateChanged(ent.Owner, ent.Comp, ref args);

    private void OnStateChanged<T>(EntityUid uid, T comp, ref MobStateChangedEvent args) where T: Component
    {
        // client would chud out without this
        if (_timing.ApplyingState)
            return;

        // not deferring removal so there is never a tick where multiple state components exist at the same time
        RemComp(uid, comp);
        AddState(uid, args.NewMobState);
    }

    [SubscribeLocalEvent]
    private void OnAliveInit(Entity<AliveMobComponent> ent, ref ComponentInit args)
    {
        if (!_sleepingQuery.HasComp(ent))
            EnsureComp<AwakeMobComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnAliveShutdown(Entity<AliveMobComponent> ent, ref ComponentShutdown args)
    {
        // client would chud out without this
        if (_timing.ApplyingState)
            return;

        RemComp<AwakeMobComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnSleepStateChanged(Entity<AliveMobComponent> ent, ref SleepStateChangedEvent args)
    {
        if (args.FellAsleep)
            RemComp<AwakeMobComponent>(ent);
        else // woke up while alive, must be awake
            EnsureComp<AwakeMobComponent>(ent);
    }

    /// <summary>
    /// Adds the components configured for the requested mob state.
    /// </summary>
    public void AddState(EntityUid uid, MobState state)
    {
        switch (state)
        {
            case MobState.Alive:
                EnsureComp<AliveMobComponent>(uid);
                break;
            case MobState.SoftCrit:
                EnsureComp<SoftCritMobComponent>(uid);
                break;
            case MobState.Critical:
                EnsureComp<CriticalMobComponent>(uid);
                break;
            case MobState.Dead:
                EnsureComp<DeadMobComponent>(uid);
                break;
            default:
                break;
        }
    }
}
