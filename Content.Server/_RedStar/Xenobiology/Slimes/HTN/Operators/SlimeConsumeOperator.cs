using System.Threading;
using System.Threading.Tasks;
using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Server._RedStar.Xenobiology.Slimes.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.DoAfter;
using Robust.Shared.Map;

namespace Content.Server._RedStar.Xenobiology.Slimes.HTN.Operators;

/// <summary>
/// Consumes the same target retained by the standard movement and melee tasks.
/// </summary>
public sealed partial class SlimeConsumeOperator : HTNOperator
{
    [Dependency] private IEntityManager _entities = default!;
    private SlimeDigestionSystem _digestion = default!;
    private SharedDoAfterSystem _doAfter = default!;
    private EntityQuery<SlimeDigestionComponent> _digestionQuery;

    // MeleeOperator clears Target; movement coordinates still reference that entity.
    private const string TargetCoordinatesKey = "TargetCoordinates";

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _digestion = sysManager.GetEntitySystem<SlimeDigestionSystem>();
        _doAfter = sysManager.GetEntitySystem<SharedDoAfterSystem>();
        _digestionQuery = _entities.GetEntityQuery<SlimeDigestionComponent>();
    }

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var valid = blackboard.TryGetValue<EntityCoordinates>(TargetCoordinatesKey, out _, _entities);
        return Task.FromResult<(bool, Dictionary<string, object>?)>((valid, null));
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!blackboard.TryGetValue<EntityCoordinates>(TargetCoordinatesKey, out var coordinates, _entities) ||
            !_digestionQuery.TryComp(owner, out var digestion))
            return HTNOperatorStatus.Failed;

        var target = coordinates.EntityId;
        if (digestion.Stomach.ContainedEntity == target)
            return HTNOperatorStatus.Finished;

        return _doAfter.IsRunning(digestion.ConsumeDoAfter) || _digestion.TryConsume((owner, digestion), target)
            ? HTNOperatorStatus.Continuing
            : HTNOperatorStatus.Failed;
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
        => blackboard.Remove<EntityCoordinates>(TargetCoordinatesKey);

    public override void PlanShutdown(NPCBlackboard blackboard)
        => blackboard.Remove<EntityCoordinates>(TargetCoordinatesKey);
}
