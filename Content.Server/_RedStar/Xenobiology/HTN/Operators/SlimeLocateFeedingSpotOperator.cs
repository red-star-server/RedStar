using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared.Interaction;

namespace Content.Server._RedStar.Xenobiology.HTN.Operators;

public sealed partial class SlimeLocateFeedingSpotOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private SlimeBrainSystem _slimeBrainSystem = default!;
    private PathfindingSystem _pathfinding = default!;
    private EntityQuery<SlimeComponent> _slimeQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    /// <summary>
    /// Target entitycoordinates to move to.
    /// </summary>
    [DataField(required: true)]
    public string TargetMoveKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _slimeBrainSystem = sysManager.GetEntitySystem<SlimeBrainSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _slimeQuery = _entManager.GetEntityQuery<SlimeComponent>();
        _transformQuery = _entManager.GetEntityQuery<TransformComponent>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_slimeQuery.HasComp(owner) || !_transformQuery.TryComp(owner, out var slimeTransform))
            return (false, null);

        foreach (var spot in _slimeBrainSystem.AcquireFeedingSpots())
        {
            const float pathRange = SharedInteractionSystem.InteractionRange - 1f;
            var path = await _pathfinding.GetPath(owner, slimeTransform.Coordinates, spot, pathRange, cancelToken);

            if (path.Result == PathResult.NoPath)
                continue;

            return (true, new Dictionary<string, object>()
            {
                { TargetMoveKey, spot },
                { NPCBlackboard.PathfindKey, path },
            });
        }

        return (false, null);
    }
}
