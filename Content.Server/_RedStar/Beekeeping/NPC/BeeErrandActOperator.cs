using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server._RedStar.Beekeeping.Systems;

namespace Content.Server._RedStar.Beekeeping.NPC;

/// <summary>
/// A bee on an errand reached its stop: <see cref="BeeErrandSystem"/> does there what the errand wants (a visit to the
/// flower, a trip through the pipes, a bite, landing in the hive) and says when it is done.
/// </summary>
public sealed partial class BeeErrandActOperator : HTNOperator
{
    [Dependency] private BeeErrandSystem _errands = default!;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        _errands.StartAct(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        return _errands.UpdateAct(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
    }
}
