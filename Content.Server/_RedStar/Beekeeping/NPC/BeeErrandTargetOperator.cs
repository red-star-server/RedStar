using System.Threading;
using System.Threading.Tasks;
using Content.Server._RedStar.Beekeeping.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server._RedStar.Beekeeping.NPC;

/// <summary>
/// Asks <see cref="BeeErrandSystem"/> where a bee on an errand flies next (the tray, a vent, a nest, home) and puts it
/// where the move operator after it reads it. Fails when there is nowhere to go: the bee then idles.
/// </summary>
public sealed partial class BeeErrandTargetOperator : HTNOperator
{
    [Dependency] private BeeErrandSystem _errands = default!;

    [DataField]
    public string TargetKey = "TargetCoordinates";

    [DataField]
    public string RangeKey = "MovementRange";

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_errands.TryGetNextStop(owner, out var coordinates, out var range))
            return (false, null);

        return (true, new Dictionary<string, object>
        {
            { TargetKey, coordinates },
            { RangeKey, range },
        });
    }
}
