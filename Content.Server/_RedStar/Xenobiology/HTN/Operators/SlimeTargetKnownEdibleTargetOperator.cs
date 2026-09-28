using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared.Interaction;

namespace Content.Server._RedStar.Xenobiology.HTN.Operators;

public sealed partial class SlimeTargetKnownEdibleTargetOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private SlimeBrainSystem _slimeBrainSystem = default!;
    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;
    private EntityQuery<SlimeComponent> _slimeQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    /// <summary>
    /// Target entity to eat.
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Target entitycoordinates to move to.
    /// </summary>
    [DataField(required: true)]
    public string TargetMoveKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _slimeBrainSystem = sysManager.GetEntitySystem<SlimeBrainSystem>();
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _slimeQuery = _entManager.GetEntityQuery<SlimeComponent>();
        _transformQuery = _entManager.GetEntityQuery<TransformComponent>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_slimeQuery.HasComp(owner))
            return (false, null);

        var targets = _slimeBrainSystem.AcquireTargetFoods();
        foreach (var entity in _lookup.GetEntitiesInRange(owner, _slimeBrainSystem.FoodSearchRange))
        {
            if (!targets.Contains(entity) || !_slimeBrainSystem.IsEdibleBySlimeTest(entity))
                continue;

            const float pathRange = SharedInteractionSystem.InteractionRange - 1f;
            var path = await _pathfinding.GetPath(owner, entity, pathRange, cancelToken);

            if (path.Result == PathResult.NoPath || !_slimeBrainSystem.IsEdibleBySlimeTest(entity) ||
                !_transformQuery.TryComp(entity, out var transform))
                continue;

            return (true, new Dictionary<string, object>()
            {
                {TargetKey, entity},
                {TargetMoveKey, transform.Coordinates},
                {NPCBlackboard.PathfindKey, path},
            });
        }

        return (false, null);
    }
}
