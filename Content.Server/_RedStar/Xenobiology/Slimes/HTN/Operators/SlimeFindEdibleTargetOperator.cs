using System.Threading;
using System.Threading.Tasks;
using Content.Server._RedStar.Xenobiology.Slimes.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared._RedStar.Xenobiology.Slimes.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Xenobiology.Slimes.HTN.Operators;

/// <summary>
/// Adds nearby edible targets to the shared slime brain so slimes converge on known food.
/// Discovery does not require a path, allowing slimes to detect food through walls.
/// </summary>
public sealed partial class SlimeFindEdibleTargetOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private SlimeBrainSystem _slimeBrainSystem = default!;
    private EntityLookupSystem _lookup = default!;
    private TagSystem _tagSystem = default!;
    private EntityQuery<SlimeComponent> _slimeQuery;

    /// <summary>
    /// The tag an entity must have in order to be considered safe to eat (not desperate).
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype> TargetFoodTag;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _slimeBrainSystem = sysManager.GetEntitySystem<SlimeBrainSystem>();
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _tagSystem = sysManager.GetEntitySystem<TagSystem>();
        _slimeQuery = _entManager.GetEntityQuery<SlimeComponent>();
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_slimeQuery.HasComp(owner))
            return (false, null);

        foreach (var entity in _lookup.GetEntitiesInRange(owner, SlimeBrainSystem.FoodSearchRange))
        {
            if (!_tagSystem.HasTag(entity, TargetFoodTag))
                continue;

            if (_slimeBrainSystem.TryAddTargetFood(entity))
                return (true, null);
        }

        _slimeBrainSystem.SlimeUnsuccessfulFoodFind(owner);
        return (false, null);
    }
}
