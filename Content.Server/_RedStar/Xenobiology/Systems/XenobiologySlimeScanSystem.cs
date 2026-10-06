using Content.Server._RedStar.AnimalHusbandry;
using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Shared._RedStar.Xenobiology.Components;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class XenobiologySlimeScanSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<SatiationComponent> _satiationQuery;

    [SubscribeLocalEvent]
    private void OnScan(Entity<XenobiologyScannableComponent> ent, ref XenobiologyScanEvent args)
    {
        if (!HasComp<SlimeComponent>(ent.Owner))
            return;

        if (_satiationQuery.TryComp(ent.Owner, out var satiation) &&
            _satiation.GetValueOrNull((ent.Owner, satiation), SatiationSystem.Hunger) is { } hunger)
            args.Nutrition = new XenobiologyNutritionScanData(hunger);

        var development = new XenobiologyDevelopmentScanData(false, 0f);
        if (TryComp<TimedMetamorphosisComponent>(ent.Owner, out var maturing))
        {
            var progress = maturing.Duration > TimeSpan.Zero
                ? Math.Clamp(1f - (float) ((maturing.EndTime - _timing.CurTime) / maturing.Duration), 0f, 1f)
                : 0f;
            development = new XenobiologyDevelopmentScanData(true, progress);
        }
        else if (TryComp<SlimeMitosisComponent>(ent.Owner, out var mitosis) &&
                 mitosis.GestationEnd is { } end && mitosis.GestationDuration > TimeSpan.Zero)
        {
            var progress = Math.Clamp(1f - (float) ((end - _timing.CurTime) / mitosis.GestationDuration), 0f, 1f);
            development = new XenobiologyDevelopmentScanData(false, progress);
        }

        args.Development = development;

        if (!TryComp<SlimeMutationComponent>(ent.Owner, out var mutation))
            return;

        for (var i = 0; i < mutation.Mutations.Count; i++)
        {
            var route = mutation.Mutations[i];
            var target = route.Target;
            if (ProtoMan.TryIndex<EntityPrototype>(route.Target, out var prototype) &&
                prototype.TryComp<TimedMetamorphosisComponent>(out var metamorphosis, Factory))
                target = metamorphosis.Target;

            var progress = route.RequiredProgress > 0 && float.IsFinite(route.RequiredProgress) &&
                           i < mutation.MutationProgress.Length && float.IsFinite(mutation.MutationProgress[i])
                ? Math.Clamp(mutation.MutationProgress[i] / route.RequiredProgress, 0f, 1f)
                : 0f;
            args.Mutations.Add(new(target, progress));
        }
    }
}
