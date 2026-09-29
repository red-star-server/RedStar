using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Server._RedStar.Xenobiology.Slimes.Systems;
using Content.Shared._RedStar.Xenobiology.Slimes;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class SlimeScanSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SlimeHusbandrySystem _husbandry = default!;

    [Dependency] private EntityQuery<SlimeLifecycleComponent> _lifecycleQuery;
    [Dependency] private EntityQuery<SatiationComponent> _satiationQuery;
    [Dependency] private EntityQuery<SlimeHusbandryComponent> _husbandryQuery;

    public SlimeScanData? TryBuildSlimeScanData(Entity<SlimeLifecycleComponent?> ent)
    {
        if (!_lifecycleQuery.Resolve(ent, ref ent.Comp, false) || ent.Comp is not { } lifecycle)
            return null;

        var uid = ent.Owner;

        float? hunger = null;
        if (_satiationQuery.TryComp(uid, out var satiation))
            hunger = _satiation.GetValueOrNull((uid, satiation), SatiationSystem.Hunger);

        var mutations = new EntProtoId[lifecycle.Mutations.Count];
        for (var i = 0; i < mutations.Length; i++)
        {
            mutations[i] = lifecycle.Mutations[i].Target;
        }

        var growth = lifecycle.GrowthThreshold > 0
            ? Math.Clamp(lifecycle.Growth / lifecycle.GrowthThreshold, 0f, 1f)
            : 0f;

        var temperament = SlimeTemperament.Calm;
        var crowding = SlimeCrowding.Low;
        if (!_husbandryQuery.TryComp(uid, out var husbandry))
        {
            return new SlimeScanData(MetaData(uid).EntityName, growth, hunger,
                lifecycle.MutationChance.Float(), mutations,
                HasComp<SlimeExtractYieldEnhancedComponent>(uid), temperament, crowding,
                MetaData(uid).EntityPrototype?.ID, lifecycle.Stage);
        }

        temperament = husbandry.Temperament;
        crowding = _husbandry.GetCrowding((uid, husbandry));

        return new SlimeScanData(MetaData(uid).EntityName, growth, hunger,
            lifecycle.MutationChance.Float(), mutations,
            HasComp<SlimeExtractYieldEnhancedComponent>(uid), temperament, crowding,
            MetaData(uid).EntityPrototype?.ID, lifecycle.Stage);
    }
}
