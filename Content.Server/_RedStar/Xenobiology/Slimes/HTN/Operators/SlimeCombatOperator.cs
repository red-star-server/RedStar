using System.Threading;
using System.Threading.Tasks;
using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Server._RedStar.Xenobiology.Slimes.Systems;
using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat.Melee;
using Content.Server.NPC.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;

namespace Content.Server._RedStar.Xenobiology.Slimes.HTN.Operators;

/// <summary>
/// Keeps the ordinary melee target until combat ends or that same target is consumed.
/// </summary>
public sealed partial class SlimeCombatOperator : HTNOperator
{
    [Dependency] private IEntityManager _entities = default!;
    private readonly MeleeOperator _melee = new() { TargetKey = NPCBlackboard.Target };
    private SlimeDigestionSystem _digestion = default!;
    private SharedDoAfterSystem _doAfter = default!;
    private NPCSteeringSystem _steering = default!;
    private EntityQuery<SlimeDigestionComponent> _digestionQuery;
    private EntityQuery<MobStateComponent> _mobQuery;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _melee.Initialize(sysManager);
        _digestion = sysManager.GetEntitySystem<SlimeDigestionSystem>();
        _doAfter = sysManager.GetEntitySystem<SharedDoAfterSystem>();
        _steering = sysManager.GetEntitySystem<NPCSteeringSystem>();
        _digestionQuery = _entities.GetEntityQuery<SlimeDigestionComponent>();
        _mobQuery = _entities.GetEntityQuery<MobStateComponent>();
    }

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken) => _melee.Plan(blackboard, cancelToken);

    public override void Startup(NPCBlackboard blackboard) => _melee.Startup(blackboard);

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Target, out var target, _entities) ||
            !_digestionQuery.TryComp(owner, out var digestion) || !_mobQuery.TryComp(target, out var state))
            return HTNOperatorStatus.Failed;

        if (digestion.Stomach.ContainedEntity == target)
            return HTNOperatorStatus.Finished;

        if (state.CurrentState == MobState.Alive)
        {
            if (!_entities.HasComponent<NPCMeleeCombatComponent>(owner))
                _melee.Startup(blackboard);
            return _melee.Update(blackboard, frameTime);
        }

        _entities.RemoveComponent<NPCMeleeCombatComponent>(owner);
        if (state.CurrentState != MobState.Critical)
            return HTNOperatorStatus.Finished;

        if (_doAfter.IsRunning(digestion.ConsumeDoAfter) || _digestion.TryConsume((owner, digestion), target))
        {
            _steering.Unregister(owner);
            return HTNOperatorStatus.Continuing;
        }

        // Reuse this combat target, including when it falls outside consume range.
        if (digestion.Stomach.ContainedEntity != null || !_digestion.IsConsumableVictim(target))
            return HTNOperatorStatus.Failed;

        _steering.Register(owner, new EntityCoordinates(target, System.Numerics.Vector2.Zero));
        return HTNOperatorStatus.Continuing;
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
        => _melee.TaskShutdown(blackboard, status);

    public override void PlanShutdown(NPCBlackboard blackboard) => _melee.PlanShutdown(blackboard);
}
