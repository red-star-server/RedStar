using Content.Shared._RedStar.Xenobiology;
using Content.Shared._Starlight.Xenobiology;
using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology;

public sealed partial class SlimeProcessorSystem : EntitySystem
{
    [Dependency] private EntityManager _entityManager = default!;
    [Dependency] private SharedJitteringSystem _jitteringSystem = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<SlimeProcessorComponent> ent, ref ComponentInit args)
    {
        ent.Comp.SlimeContainer = _container.EnsureContainer<Container>(ent, SlimeProcessorComponent.SlimeContainerName);
    }

    [SubscribeLocalEvent]
    private void OnAfterActivate(Entity<SlimeProcessorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (CanActivate(ent))
            EnableProcessingWrapper(ent);
    }

    [SubscribeLocalEvent]
    private void OnGetVerb(Entity<SlimeProcessorComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var itemVerb = new InteractionVerb
        {
            Text = Loc.GetString("comp-slime-processor-verb-activate")
        };
        if (CanActivate(ent))
        {
            itemVerb.Message = Loc.GetString("comp-slime-processor-verb-activate-message-success");
        }
        else
        {
            itemVerb.Disabled = true;
            itemVerb.Message = Loc.GetString("comp-slime-processor-verb-activate-message-no-slimes");
        }
        itemVerb.Act = () => EnableProcessingWrapper(ent);
        args.Verbs.Add(itemVerb);
    }

    private void EnableProcessingWrapper(Entity<SlimeProcessorComponent> ent)
    {
        EnableProcessing(ent, _entityManager, _gameTiming);
        _jitteringSystem.AddJitter(ent.Owner, -10, 100);
        _audioSystem.PlayPvs(new SoundPathSpecifier("/Audio/Machines/blender.ogg"), ent.Owner);
    }

    private bool CanActivate(Entity<SlimeProcessorComponent> ent) =>
        ent.Comp.SlimeContainer.ContainedEntities.Count > 0 && !HasComp<ActiveSlimeProcessorComponent>(ent);

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<SlimeProcessorComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            if (HasComp<ActiveSlimeProcessorComponent>(ent.Owner))
                RemCompDeferred<ActiveSlimeProcessorComponent>(ent.Owner);
            if (HasComp<CollectingSlimeProcessorComponent>(ent.Owner))
                RemCompDeferred<CollectingSlimeProcessorComponent>(ent.Owner);
            if (HasComp<JitteringComponent>(ent.Owner))
                RemCompDeferred<JitteringComponent>(ent.Owner);
        }
        else
        {
            EnableCollecting(ent, _entityManager, _gameTiming);
        }
    }

    public static void EnableCollecting(Entity<SlimeProcessorComponent> ent, EntityManager entityManager, IGameTiming gameTiming)
    {
        if (entityManager.HasComponent<CollectingSlimeProcessorComponent>(ent.Owner))
            return;

        if (entityManager.HasComponent<ActiveSlimeProcessorComponent>(ent))
            entityManager.RemoveComponentDeferred<ActiveSlimeProcessorComponent>(ent);

        var collectingSlimeProcessorComponent = entityManager.AddComponent<CollectingSlimeProcessorComponent>(ent.Owner);
        collectingSlimeProcessorComponent.SlimeAcquireMoment = gameTiming.CurTime + ent.Comp.SlimeAcquireCooldown;
    }

    public static void EnableProcessing(Entity<SlimeProcessorComponent> ent, EntityManager entityManager, IGameTiming gameTiming)
    {
        if (entityManager.HasComponent<ActiveSlimeProcessorComponent>(ent.Owner))
            return;

        if (entityManager.HasComponent<CollectingSlimeProcessorComponent>(ent))
            entityManager.RemoveComponentDeferred<CollectingSlimeProcessorComponent>(ent);

        var activeSlimeProcessorComponent = entityManager.AddComponent<ActiveSlimeProcessorComponent>(ent);
        activeSlimeProcessorComponent.ProcessingFinishedMoment = gameTiming.CurTime + ent.Comp.ProcessingTime;
    }
}

public sealed partial class ActiveSlimeProcessorSystem : EntitySystem
{
    [Dependency] private EntityManager _entityManager = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveSlimeProcessorComponent, SlimeProcessorComponent>();
        while (query.MoveNext(out var uid, out var activeSlimeProcessorComponent, out var slimeProcessorComponent))
        {
            if (!activeSlimeProcessorComponent.ProcessingFinishedMoment.HasValue)
            {
                activeSlimeProcessorComponent.ProcessingFinishedMoment = _gameTiming.CurTime + slimeProcessorComponent.ProcessingTime;
                continue;
            }

            if (activeSlimeProcessorComponent.ProcessingFinishedMoment.Value > _gameTiming.CurTime)
                continue;

            foreach (var entity in slimeProcessorComponent.SlimeContainer.ContainedEntities)
            {
                if (!_entityManager.TryGetComponent(entity, out SlimeComponent? slimeComponent))
                    continue;

                if (TryComp<SlimeLifecycleComponent>(entity, out var lifecycle) &&
                    lifecycle.Stage != SlimeStage.Adult)
                {
                    QueueDel(entity);
                    continue;
                }

                Spawn(slimeComponent.Extract, uid.ToCoordinates());
                if (HasComp<SlimeExtractYieldEnhancedComponent>(entity))
                    Spawn(slimeComponent.Extract, uid.ToCoordinates());
                QueueDel(entity);
            }

            RemCompDeferred<JitteringComponent>(uid);
            RemCompDeferred<ActiveSlimeProcessorComponent>(uid);
            SlimeProcessorSystem.EnableCollecting((uid, slimeProcessorComponent), _entityManager, _gameTiming);
        }
    }
}

public sealed partial class CollectingSlimeProcessorSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CollectingSlimeProcessorComponent, SlimeProcessorComponent>();
        while (query.MoveNext(out var uid, out var collectingSlimeProcessorComponent, out var slimeProcessorComponent))
        {
            slimeProcessorComponent.SlimeContainer = _container.EnsureContainer<Container>(uid, SlimeProcessorComponent.SlimeContainerName);
            if (!collectingSlimeProcessorComponent.SlimeAcquireMoment.HasValue)
            {
                collectingSlimeProcessorComponent.SlimeAcquireMoment = _gameTiming.CurTime + slimeProcessorComponent.SlimeAcquireCooldown;
                continue;
            }

            if (collectingSlimeProcessorComponent.SlimeAcquireMoment.Value > _gameTiming.CurTime)
                continue;

            foreach (var entity in _entityLookupSystem.GetEntitiesInRange<SlimeComponent>(Transform(uid).Coordinates, 1F))
            {
                if (_container.IsEntityOrParentInContainer(entity.Owner))
                    continue;

                if (!_mobState.IsDead(entity.Owner))
                    continue;

                _container.Insert(entity.Owner, slimeProcessorComponent.SlimeContainer);
                collectingSlimeProcessorComponent.SlimeAcquireMoment = _gameTiming.CurTime + slimeProcessorComponent.SlimeAcquireCooldown;
                break;
            }
        }
    }
}
