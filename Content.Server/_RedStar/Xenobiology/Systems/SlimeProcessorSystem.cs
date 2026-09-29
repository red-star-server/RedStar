using Content.Server._RedStar.Xenobiology.Components;
using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Xenobiology.Slimes;
using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class SlimeProcessorSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [Dependency] private EntityQuery<ActiveSlimeProcessorComponent> _activeQuery;
    [Dependency] private EntityQuery<SlimeProcessorComponent> _processorQuery;
    [Dependency] private EntityQuery<SlimeComponent> _slimeQuery;
    [Dependency] private EntityQuery<SlimeLifecycleComponent> _lifecycleQuery;

    [SubscribeLocalEvent]
    private void OnInit(Entity<SlimeProcessorComponent> ent, ref ComponentInit args)
    {
        ent.Comp.SlimeContainer = _container.EnsureContainer<Container>(ent, SlimeProcessorComponent.SlimeContainerName);
        ent.Comp.NextSlimeAcquireTime = _timing.CurTime + ent.Comp.SlimeAcquireCooldown;
    }

    [SubscribeLocalEvent]
    private void OnAfterActivate(Entity<SlimeProcessorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartProcessing(ent);
    }

    [SubscribeLocalEvent]
    private void OnGetVerb(Entity<SlimeProcessorComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !this.IsPowered(ent, EntityManager))
            return;

        var canActivate = CanActivate(ent);
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("comp-slime-processor-verb-activate"),
            Disabled = !canActivate,
            Message = Loc.GetString(canActivate
                ? "comp-slime-processor-verb-activate-message-success"
                : "comp-slime-processor-verb-activate-message-no-slimes"),
            Act = () => TryStartProcessing(ent)
        });
    }

    private bool CanActivate(Entity<SlimeProcessorComponent> ent) =>
        this.IsPowered(ent, EntityManager) &&
        ent.Comp.SlimeContainer.ContainedEntities.Count > 0 && !_activeQuery.HasComp(ent);

    private bool TryStartProcessing(Entity<SlimeProcessorComponent> ent)
    {
        if (!CanActivate(ent))
            return false;

        var active = AddComp<ActiveSlimeProcessorComponent>(ent);
        active.ProcessingEndTime = _timing.CurTime + ent.Comp.ProcessingTime;
        return true;
    }

    [SubscribeLocalEvent]
    private void OnProcessingStarted(Entity<ActiveSlimeProcessorComponent> ent, ref ComponentStartup args)
    {
        if (!_processorQuery.TryComp(ent, out var processor))
            return;

        if (!this.IsPowered(ent, EntityManager))
            ent.Comp.PowerLossTime ??= _timing.CurTime;

        if (ent.Comp.PowerLossTime == null)
            StartRunningEffects((ent.Owner, processor));
    }

    [SubscribeLocalEvent]
    private void OnProcessingStopped(Entity<ActiveSlimeProcessorComponent> ent, ref ComponentShutdown args)
    {
        if (_processorQuery.TryComp(ent, out var processor))
            StopRunningEffects((ent.Owner, processor));
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<SlimeProcessorComponent> ent, ref PowerChangedEvent args)
    {
        if (!_activeQuery.TryComp(ent, out var active))
            return;

        if (!args.Powered)
        {
            active.PowerLossTime ??= _timing.CurTime;
            StopRunningEffects(ent);
        }
        else if (active.PowerLossTime is { } powerLossTime)
        {
            active.ProcessingEndTime += _timing.CurTime - powerLossTime;
            active.PowerLossTime = null;
            StartRunningEffects(ent);
        }
    }

    private void StartRunningEffects(Entity<SlimeProcessorComponent> ent)
    {
        _appearance.SetData(ent.Owner, SlimeProcessorVisuals.Processing, true);
        ent.Comp.AudioStream = _audio.PlayPvs(ent.Comp.ProcessingSound, ent)?.Entity;
    }

    private void StopRunningEffects(Entity<SlimeProcessorComponent> ent)
    {
        _appearance.SetData(ent.Owner, SlimeProcessorVisuals.Processing, false);
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SlimeProcessorComponent>();
        while (query.MoveNext(out var uid, out var processor))
        {
            if (!this.IsPowered(uid, EntityManager))
                continue;

            if (_activeQuery.TryComp(uid, out var active))
            {
                if (active.PowerLossTime == null && _timing.CurTime >= active.ProcessingEndTime)
                    FinishProcessing((uid, processor));
                continue;
            }

            if (_timing.CurTime < processor.NextSlimeAcquireTime)
                continue;

            processor.NextSlimeAcquireTime = _timing.CurTime + processor.SlimeAcquireCooldown;
            CollectSlime((uid, processor));
        }
    }

    private void CollectSlime(Entity<SlimeProcessorComponent> ent)
    {
        foreach (var slime in _lookup.GetEntitiesInRange<SlimeComponent>(Transform(ent).Coordinates, 1f))
        {
            if (TerminatingOrDeleted(slime.Owner) || EntityManager.IsQueuedForDeletion(slime.Owner) ||
                _container.IsEntityOrParentInContainer(slime.Owner) || !_mobState.IsDead(slime.Owner))
                continue;

            if (_container.Insert(slime.Owner, ent.Comp.SlimeContainer))
                break;
        }
    }

    private void FinishProcessing(Entity<SlimeProcessorComponent> ent)
    {
        foreach (var uid in ent.Comp.SlimeContainer.ContainedEntities)
        {
            if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid) ||
                !_slimeQuery.TryComp(uid, out var slime))
                continue;

            if (_lifecycleQuery.TryComp(uid, out var lifecycle) && lifecycle.Stage != SlimeStage.Adult)
            {
                QueueDel(uid);
                continue;
            }

            Spawn(slime.Extract, ent.Owner.ToCoordinates());
            QueueDel(uid);
        }

        StopRunningEffects(ent);
        RemCompDeferred<ActiveSlimeProcessorComponent>(ent);
        ent.Comp.NextSlimeAcquireTime = _timing.CurTime + ent.Comp.SlimeAcquireCooldown;
    }
}
