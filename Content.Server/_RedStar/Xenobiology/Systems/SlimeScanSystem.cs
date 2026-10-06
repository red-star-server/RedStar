using Content.Server._RedStar.AnimalHusbandry;
using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Shared._RedStar.Xenobiology.Slimes;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class SlimeScanSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<SlimeComponent> _slimeQuery;
    [Dependency] private EntityQuery<SatiationComponent> _satiationQuery;

    public SlimeScanData? TryBuildSlimeScanData(Entity<SlimeComponent?> ent)
    {
        if (!_slimeQuery.Resolve(ent, ref ent.Comp, false))
            return null;

        var uid = ent.Owner;
        float? hunger = null;
        if (_satiationQuery.TryComp(uid, out var satiation))
            hunger = _satiation.GetValueOrNull((uid, satiation), SatiationSystem.Hunger);

        var mutations = Array.Empty<SlimeMutationScanEntry>();
        if (TryComp<SlimeMutationComponent>(uid, out var mutation))
        {
            mutations = new SlimeMutationScanEntry[mutation.Mutations.Count];
            for (var i = 0; i < mutation.Mutations.Count; i++)
            {
                var route = mutation.Mutations[i];
                var baby = route.Target;
                var target = ProtoMan.TryIndex<EntityPrototype>(baby, out var prototype) &&
                               prototype.TryComp<TimedMetamorphosisComponent>(out var metamorphosis, Factory)
                    ? metamorphosis.Target
                    : baby;
                var progress = route.RequiredProgress > 0 && float.IsFinite(route.RequiredProgress) &&
                               i < mutation.MutationProgress.Length && float.IsFinite(mutation.MutationProgress[i])
                    ? Math.Clamp(mutation.MutationProgress[i] / route.RequiredProgress, 0f, 1f)
                    : 0f;
                mutations[i] = new SlimeMutationScanEntry(target, progress);
            }
        }

        var stage = SlimeStage.Adult;
        var growth = 0f;
        if (TryComp<TimedMetamorphosisComponent>(uid, out var maturing))
        {
            stage = SlimeStage.Baby;
            if (maturing.Duration > TimeSpan.Zero)
                growth = Math.Clamp(1f - (float) ((maturing.EndTime - _timing.CurTime) / maturing.Duration), 0f, 1f);
        }
        else if (TryComp<SlimeMitosisComponent>(uid, out var mitosis) &&
                 mitosis.GestationEnd is { } end && mitosis.GestationDuration > TimeSpan.Zero)
        {
            growth = Math.Clamp(1f - (float) ((end - _timing.CurTime) / mitosis.GestationDuration), 0f, 1f);
        }

        return new SlimeScanData(MetaData(uid).EntityName, growth, hunger, mutations,
            MetaData(uid).EntityPrototype?.ID, stage);
    }
}
