using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Shared.EntityConditions;
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

    [SubscribeLocalEvent]
    private void OnInit(Entity<SlimeMutationComponent> ent, ref ComponentInit args)
    {
        ent.Comp.MutationProgress = new float[ent.Comp.Mutations.Count];
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _elapsed += frameTime;
        if (_elapsed < UpdateInterval)
            return;

        var elapsed = _elapsed;
        _elapsed = 0f;
        var query = EntityQueryEnumerator<SlimeMutationComponent>();
        while (query.MoveNext(out var uid, out var mutation))
        {
            if (mutation.MutationProgress.Length != mutation.Mutations.Count)
                mutation.MutationProgress = new float[mutation.Mutations.Count];

            for (var i = 0; i < mutation.Mutations.Count; i++)
            {
                var route = mutation.Mutations[i];
                if (route.ProgressRate <= 0 || !float.IsFinite(route.ProgressRate) ||
                    route.RequiredProgress <= 0 || !float.IsFinite(route.RequiredProgress) ||
                    mutation.MutationProgress[i] >= route.RequiredProgress ||
                    !_conditions.TryConditions(uid, route.Conditions))
                    continue;

                mutation.MutationProgress[i] = Math.Min(route.RequiredProgress,
                    mutation.MutationProgress[i] + route.ProgressRate * elapsed);
            }
        }
    }

    public string ResolveOffspring(Entity<SlimeMutationComponent> ent, string prototype)
    {
        var mutation = ent.Comp;
        if (mutation.MutationProgress.Length != mutation.Mutations.Count)
            mutation.MutationProgress = new float[mutation.Mutations.Count];

        var total = 0f;
        for (var i = 0; i < mutation.Mutations.Count; i++)
        {
            var route = mutation.Mutations[i];
            if (IsEligible(route, mutation.MutationProgress[i]))
                total += route.Weight;
        }

        if (total <= 0 || !float.IsFinite(total))
            return prototype;

        var roll = _random.NextFloat() * total;
        for (var i = 0; i < mutation.Mutations.Count; i++)
        {
            var route = mutation.Mutations[i];
            if (!IsEligible(route, mutation.MutationProgress[i]))
                continue;

            roll -= route.Weight;
            if (roll >= 0)
                continue;

            return route.Target.Id;
        }

        return prototype;
    }

    private static bool IsEligible(SlimeMutationEntry route, float progress)
        => route.Weight > 0 && float.IsFinite(route.Weight) &&
           route.ProgressRate > 0 && float.IsFinite(route.ProgressRate) &&
           route.RequiredProgress > 0 && float.IsFinite(route.RequiredProgress) &&
           progress >= route.RequiredProgress;
}
