using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Shared._RedStar.Xenobiology.Slimes;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Server._RedStar.Xenobiology.Slimes.Systems;

public sealed partial class SlimeHusbandrySystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SharedContainerSystem _containers = default!;

    private const float UpdateInterval = 1.5f;
    private float _updateAccumulator;
    private readonly HashSet<Entity<SlimeComponent>> _nearby = [];

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _updateAccumulator += frameTime;
        if (_updateAccumulator < UpdateInterval)
            return;

        _updateAccumulator = 0f;

        var query = EntityQueryEnumerator<SlimeHusbandryComponent, SlimeComponent, SatiationComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var husbandry, out _, out var satiation, out var transform))
        {
            if (_mobState.IsDead(uid))
            {
                husbandry.NearbySlimes = 0;
                husbandry.Temperament = SlimeTemperament.Calm;
                continue;
            }

            husbandry.NearbySlimes = CountNearbySlimes((uid, transform), husbandry.OvercrowdingRadius);

            var severeHunger = _satiation.IsValueInRange((uid, satiation), SatiationSystem.Hunger,
                below: husbandry.AggressiveBelow);
            var moderateHunger = _satiation.IsValueInRange((uid, satiation), SatiationSystem.Hunger,
                below: husbandry.RestlessBelow);

            if (husbandry.NearbySlimes >= husbandry.AggressiveThreshold || severeHunger)
                husbandry.Temperament = SlimeTemperament.Aggressive;
            else if (husbandry.NearbySlimes >= husbandry.RestlessThreshold || moderateHunger || _mobState.IsCritical(uid))
                husbandry.Temperament = SlimeTemperament.Restless;
            else
                husbandry.Temperament = SlimeTemperament.Calm;
        }
    }

    private int CountNearbySlimes(Entity<TransformComponent> ent, float radius)
    {
        if (radius <= 0f || _containers.IsEntityOrParentInContainer(ent.Owner))
            return 0;

        _nearby.Clear();
        _lookup.GetEntitiesInRange(ent.Comp.Coordinates, radius, _nearby, LookupFlags.Uncontained);

        var count = 0;
        foreach (var candidate in _nearby)
        {
            if (candidate.Owner != ent.Owner && _mobState.IsAlive(candidate.Owner))
                count++;
        }

        return count;
    }

    public SlimeCrowding GetCrowding(Entity<SlimeHusbandryComponent> ent)
    {
        if (ent.Comp.NearbySlimes >= ent.Comp.AggressiveThreshold)
            return SlimeCrowding.Severe;

        return ent.Comp.NearbySlimes >= ent.Comp.RestlessThreshold
            ? SlimeCrowding.Crowded
            : SlimeCrowding.Low;
    }
}
