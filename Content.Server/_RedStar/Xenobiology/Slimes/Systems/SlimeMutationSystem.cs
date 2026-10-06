using Content.Server._RedStar.AnimalHusbandry;
using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.AnimalHusbandry;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Xenobiology.Slimes.Systems;

/// <summary>
/// Accumulates environmental mutation progress and selects slime offspring phenotypes.
/// </summary>
public sealed partial class SlimeMutationSystem : EntitySystem
{
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private IRobustRandom _random = default!;

    private const float UpdateInterval = 1f;
    private float _elapsed;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeMutationComponent, ResolveOffspringPrototypeEvent>(OnResolveOffspring);
        SubscribeLocalEvent<SlimeMutationComponent, OffspringSpawnedEvent>(OnOffspringSpawned);
        SubscribeLocalEvent<SlimeMutationComponent, TimedMetamorphosisEvent>(OnMetamorphosis);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _elapsed += frameTime;
        if (_elapsed < UpdateInterval)
            return;

        var elapsed = _elapsed;
        _elapsed = 0f;
        var query = EntityQueryEnumerator<SlimeMutationComponent, ReproductiveComponent>();
        while (query.MoveNext(out var uid, out var mutation, out _))
        {
            if (mutation.MutationProgress.Length != mutation.Mutations.Count)
                mutation.MutationProgress = new float[mutation.Mutations.Count];

            for (var i = 0; i < mutation.Mutations.Count; i++)
            {
                var route = mutation.Mutations[i];
                if (route.ProgressRate <= 0 || route.RequiredProgress <= 0 ||
                    mutation.MutationProgress[i] >= route.RequiredProgress ||
                    !_conditions.TryConditions(uid, route.Conditions))
                    continue;

                mutation.MutationProgress[i] = Math.Min(route.RequiredProgress,
                    mutation.MutationProgress[i] + route.ProgressRate * elapsed);
            }
        }
    }

    private void OnResolveOffspring(Entity<SlimeMutationComponent> ent, ref ResolveOffspringPrototypeEvent args)
    {
        var mutation = ent.Comp;
        if (mutation.MutationProgress.Length != mutation.Mutations.Count)
            mutation.MutationProgress = new float[mutation.Mutations.Count];

        if (!_random.Prob(mutation.MutationChance.Float()))
            return;

        var total = 0f;
        for (var i = 0; i < mutation.Mutations.Count; i++)
        {
            var route = mutation.Mutations[i];
            if (route.Weight > 0 && mutation.MutationProgress[i] >= route.RequiredProgress)
                total += route.Weight;
        }

        if (total <= 0)
            return;

        var roll = _random.NextFloat() * total;
        for (var i = 0; i < mutation.Mutations.Count; i++)
        {
            var route = mutation.Mutations[i];
            if (route.Weight <= 0 || mutation.MutationProgress[i] < route.RequiredProgress)
                continue;

            roll -= route.Weight;
            if (roll >= 0)
                continue;

            args.Prototype = route.Target.Id;
            return;
        }
    }

    private void OnOffspringSpawned(Entity<SlimeMutationComponent> ent, ref OffspringSpawnedEvent args)
    {
        if (!TryComp<SlimeMutationComponent>(args.Offspring, out var child))
            return;

        var chance = ent.Comp.MutationChance.Float() +
                     _random.NextFloat(-ent.Comp.MutationVariance, ent.Comp.MutationVariance);
        SetMutationChanceUnchecked((args.Offspring, child),
            ClampMutationChance(ent, FixedPoint2.New(chance)));
    }

    private void OnMetamorphosis(Entity<SlimeMutationComponent> ent, ref TimedMetamorphosisEvent args)
    {
        if (TryComp<SlimeMutationComponent>(args.Result, out var adult))
            SetMutationChance((args.Result, adult), ent.Comp.MutationChance);
    }

    public FixedPoint2 ClampMutationChance(Entity<SlimeMutationComponent> slime, FixedPoint2 chance)
        => FixedPoint2.Clamp(chance,
            FixedPoint2.New(slime.Comp.MinimumMutationChance),
            FixedPoint2.New(slime.Comp.MaximumMutationChance));

    public bool SetMutationChance(Entity<SlimeMutationComponent> slime, FixedPoint2 chance)
        => SetMutationChanceUnchecked(slime, ClampMutationChance(slime, chance));

    public bool SetMutationChanceUnchecked(Entity<SlimeMutationComponent> slime, FixedPoint2 chance)
    {
        if (chance == slime.Comp.MutationChance)
            return false;

        slime.Comp.MutationChance = chance;
        return true;
    }

    public bool ModifyMutationChance(Entity<SlimeMutationComponent> slime, FixedPoint2 amount)
        => SetMutationChance(slime, slime.Comp.MutationChance + amount);
}
