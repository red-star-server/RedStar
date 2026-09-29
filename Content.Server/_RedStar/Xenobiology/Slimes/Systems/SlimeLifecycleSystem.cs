using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Shared._RedStar.Xenobiology.Slimes;
using Content.Shared.Coordinates;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._RedStar.Xenobiology.Slimes.Systems;

public sealed partial class SlimeLifecycleSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private SlimeMutationSystem _mutation = default!;
    [Dependency] private SlimeHusbandrySystem _husbandry = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private EntityQuery<SatiationComponent> _satiationQuery;
    [Dependency] private EntityQuery<SlimeComponent> _slimeQuery;
    [Dependency] private EntityQuery<SlimeLifecycleComponent> _lifecycleQuery;

    private const float UpdateInterval = 1f;
    private float _updateAccumulator;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _updateAccumulator += frameTime;
        if (_updateAccumulator < UpdateInterval)
            return;

        var elapsed = _updateAccumulator;
        _updateAccumulator = 0f;

        var query = EntityQueryEnumerator<SlimeLifecycleComponent, SatiationComponent>();
        while (query.MoveNext(out var uid, out var lifecycle, out var satiation))
        {
            if (_mobState.IsDead(uid))
                continue;

            Entity<SlimeLifecycleComponent> ent = (uid, lifecycle);
            if (lifecycle.Stage == SlimeStage.Adult)
                UpdateMutations(ent, elapsed);
            if (lifecycle.GrowthRate <= 0 || lifecycle.GrowthThreshold <= 0 ||
                !_satiation.IsValueInRange((uid, satiation), SatiationSystem.Hunger, above: lifecycle.RequiredSatiation))
                continue;

            var growthMultiplier = _husbandry.GetGrowthMultiplier(uid);
            if (growthMultiplier <= 0f)
                continue;

            lifecycle.Growth += lifecycle.GrowthRate * elapsed * growthMultiplier;
            if (lifecycle.Growth < lifecycle.GrowthThreshold)
                continue;

            if (lifecycle.Stage == SlimeStage.Baby)
                BecomeAdult(ent, (uid, satiation));
            else
                Split(ent);
        }
    }

    private void UpdateMutations(Entity<SlimeLifecycleComponent> ent, float elapsed)
    {
        var lifecycle = ent.Comp;
        if (lifecycle.Mutations.Count == 0)
            return;

        if (lifecycle.MutationProgress.Length != lifecycle.Mutations.Count)
            lifecycle.MutationProgress = new float[lifecycle.Mutations.Count];

        for (var i = 0; i < lifecycle.Mutations.Count; i++)
        {
            var mutation = lifecycle.Mutations[i];
            if (mutation.ProgressRate <= 0 || mutation.RequiredProgress <= 0 ||
                lifecycle.MutationProgress[i] >= mutation.RequiredProgress ||
                !_conditions.TryConditions(ent.Owner, mutation.Conditions))
                continue;

            lifecycle.MutationProgress[i] = Math.Min(mutation.RequiredProgress,
                lifecycle.MutationProgress[i] + mutation.ProgressRate * elapsed);
        }
    }

    private void BecomeAdult(Entity<SlimeLifecycleComponent> ent, Entity<SatiationComponent> babySatiation)
    {
        var hunger = _satiation.GetValueOrNull(babySatiation, SatiationSystem.Hunger);
        var adult = Spawn(ent.Comp.AdultPrototype, ent.Owner.ToCoordinates());
        if (!_slimeQuery.TryComp(adult, out _) || !_lifecycleQuery.TryComp(adult, out var adultLifecycle))
        {
            QueueDel(adult);
            return;
        }

        _mutation.SetMutationChance((adult, adultLifecycle), ent.Comp.MutationChance);
        if (hunger is { } value &&
            _satiationQuery.TryComp(adult, out var adultSatiation) &&
            _satiation.GetValueOrNull((adult, adultSatiation), SatiationSystem.Hunger) is not null)
            _satiation.SetValue((adult, adultSatiation), SatiationSystem.Hunger, value);

        if (_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
            _mind.TransferTo(mindId, adult, mind: mind);

        QueueDel(ent.Owner);
    }

    private void Split(Entity<SlimeLifecycleComponent> ent)
    {
        DebugTools.Assert(ent.Comp.OffspringCount > 0, "Slime OffspringCount must be positive.");
        var offspringCount = Math.Max(1, ent.Comp.OffspringCount);

        EntityUid? firstChild = null;
        for (var i = 0; i < offspringCount; i++)
        {
            var chance = _mutation.ClampMutationChance(ent,
                FixedPoint2.New(ent.Comp.MutationChance.Float() +
                    _random.NextFloat(-ent.Comp.MutationVariance, ent.Comp.MutationVariance)));
            var babyPrototype = SelectBabyPrototype(ent, chance);
            var child = Spawn(babyPrototype, ent.Owner.ToCoordinates());
            if (!_slimeQuery.TryComp(child, out _) || !_lifecycleQuery.TryComp(child, out var childLifecycle))
            {
                QueueDel(child);
                continue;
            }

            firstChild ??= child;
            _mutation.SetMutationChanceUnchecked((child, childLifecycle), chance);
        }

        if (firstChild == null)
            return;

        if (_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
            _mind.TransferTo(mindId, firstChild.Value, mind: mind);

        QueueDel(ent.Owner);
    }

    private EntProtoId SelectBabyPrototype(Entity<SlimeLifecycleComponent> ent, FixedPoint2 childMutationChance)
    {
        var lifecycle = ent.Comp;
        if (!_random.Prob(childMutationChance.Float()))
            return lifecycle.BabyPrototype;

        var totalWeight = 0f;
        for (var i = 0; i < lifecycle.Mutations.Count; i++)
        {
            var mutation = lifecycle.Mutations[i];
            if (mutation.Weight > 0 && lifecycle.MutationProgress[i] >= mutation.RequiredProgress)
                totalWeight += mutation.Weight;
        }

        if (totalWeight <= 0)
            return lifecycle.BabyPrototype;

        var roll = _random.NextFloat() * totalWeight;
        for (var i = 0; i < lifecycle.Mutations.Count; i++)
        {
            var mutation = lifecycle.Mutations[i];
            if (mutation.Weight <= 0 || lifecycle.MutationProgress[i] < mutation.RequiredProgress)
                continue;

            roll -= mutation.Weight;
            if (roll >= 0)
                continue;

            if (ProtoMan.TryIndex<EntityPrototype>(mutation.Target, out var target) &&
                target.TryComp<SlimeLifecycleComponent>(out var targetLifecycle, Factory))
                return targetLifecycle.BabyPrototype;

            return lifecycle.BabyPrototype;
        }

        return lifecycle.BabyPrototype;
    }
}
