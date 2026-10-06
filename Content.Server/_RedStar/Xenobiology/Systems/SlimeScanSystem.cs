using Content.Server._RedStar.AnimalHusbandry;
using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Server._RedStar.Xenobiology.Slimes.Systems;
using Content.Shared._RedStar.Xenobiology.Slimes;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class SlimeScanSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SlimeHusbandrySystem _husbandry = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<SlimeComponent> _slimeQuery;
    [Dependency] private EntityQuery<SatiationComponent> _satiationQuery;
    [Dependency] private EntityQuery<SlimeHusbandryComponent> _husbandryQuery;

    public SlimeScanData? TryBuildSlimeScanData(Entity<SlimeComponent?> ent)
    {
        if (!_slimeQuery.Resolve(ent, ref ent.Comp, false))
            return null;

        var uid = ent.Owner;
        float? hunger = null;
        if (_satiationQuery.TryComp(uid, out var satiation))
            hunger = _satiation.GetValueOrNull((uid, satiation), SatiationSystem.Hunger);

        var mutations = Array.Empty<EntProtoId>();
        var chance = 0f;
        if (TryComp<SlimeMutationComponent>(uid, out var mutation))
        {
            chance = mutation.MutationChance.Float();
            mutations = new EntProtoId[mutation.Mutations.Count];
            for (var i = 0; i < mutations.Length; i++)
            {
                var baby = mutation.Mutations[i].Target;
                mutations[i] = ProtoMan.TryIndex<EntityPrototype>(baby, out var prototype) &&
                               prototype.TryComp<TimedMetamorphosisComponent>(out var metamorphosis, Factory)
                    ? metamorphosis.Target
                    : baby;
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
        else if (TryComp<ReproductiveComponent>(uid, out var reproductive) &&
                 reproductive.GestationEndTime is { } end && reproductive.GestationDuration > TimeSpan.Zero)
        {
            growth = Math.Clamp(1f - (float) ((end - _timing.CurTime) / reproductive.GestationDuration), 0f, 1f);
        }

        var temperament = SlimeTemperament.Calm;
        var crowding = SlimeCrowding.Low;
        if (!_husbandryQuery.TryComp(uid, out var husbandry))
        {
            return new SlimeScanData(MetaData(uid).EntityName, growth, hunger, chance, mutations,
                temperament, crowding, MetaData(uid).EntityPrototype?.ID, stage);
        }

        temperament = husbandry.Temperament;
        crowding = _husbandry.GetCrowding((uid, husbandry));

        return new SlimeScanData(MetaData(uid).EntityName, growth, hunger, chance, mutations,
            temperament, crowding, MetaData(uid).EntityPrototype?.ID, stage);
    }
}
