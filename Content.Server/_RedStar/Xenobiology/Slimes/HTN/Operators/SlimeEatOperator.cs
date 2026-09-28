using Content.Server._RedStar.Xenobiology.Slimes.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared._RedStar.Xenobiology.Slimes.Components;
using Content.Shared._RedStar.Xenobiology.Slimes.Systems;

namespace Content.Server._RedStar.Xenobiology.Slimes.HTN.Operators;

public sealed partial class SlimeEatOperator : HTNOperator
{
    [Dependency] private IEntityManager _entMan = default!;
    private SlimeSystem _slimeSystem = default!;
    private SlimeBrainSystem _slimeBrainSystem = default!;
    private EntityQuery<SlimeComponent> _slimeQuery;

    /// <summary>
    /// Target entity to eat.
    /// </summary>
    [DataField(required: true)]
    public string TargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _slimeSystem = sysManager.GetEntitySystem<SlimeSystem>();
        _slimeBrainSystem = sysManager.GetEntitySystem<SlimeBrainSystem>();
        _slimeQuery = _entMan.GetEntityQuery<SlimeComponent>();
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        blackboard.Remove<EntityUid>(TargetKey);
        base.TaskShutdown(blackboard, status);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_slimeQuery.TryComp(owner, out var slime)
            || !blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entMan)
            || _entMan.Deleted(target) || !_slimeBrainSystem.IsEdibleBySlimeTest(target)
            || !_slimeSystem.TryEat((owner, slime), target))
            return HTNOperatorStatus.Failed;

        _slimeBrainSystem.SlimeSuccessfulEat(owner);

        return HTNOperatorStatus.Finished;
    }
}
