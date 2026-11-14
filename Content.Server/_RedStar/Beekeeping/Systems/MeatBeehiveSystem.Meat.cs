using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Prototypes;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// The blood-fed colony's combs and its meat. Each comb a meat hive builds takes some of the blood the bees brought;
/// when the hive is broken the combs carry that blood and a healing reagent, and meat falls out: more of it the
/// more the bees worked on corpses, and rarer kinds the more kinds of blood they brought.
/// </summary>
public sealed partial class MeatBeehiveSystem
{
    /// <summary>
    /// A new comb in a meat hive takes its share of the blood stock into the combs.
    /// </summary>
    public void TakeCombBlood(Entity<BeehiveComponent> hive)
    {
        var meatComp = MeatOf(hive);
        var comp = hive.Comp;
        var amount = FixedPoint2.Min(FixedPoint2.New(meatComp.CombBlood), meatComp.Blood.Volume);
        if (amount > FixedPoint2.Zero)
            meatComp.CombBlooded.AddSolution(meatComp.Blood.SplitSolution(amount), _proto);
    }

    /// <summary>
    /// The combs of a broken meat hive: the blood of everyone bitten, shared out between them, and a fixed healing
    /// reagent, since blood alone does nothing for most eaters.
    /// </summary>
    private void FillMeatCombs(Entity<BeehiveComponent> hive, EntityUid combs, int count)
    {
        var meatComp = MeatOf(hive);
        var comp = hive.Comp;
        if (count <= 0 || !_solutions.TryGetSolution(combs, "food", out var combSolution, out _))
            return;

        // The stack's solution is one comb's worth.
        if (meatComp.CombBlooded.Volume > FixedPoint2.Zero)
        {
            _solutions.ForceAddSolution(combSolution.Value, meatComp.CombBlooded.SplitSolution(meatComp.CombBlooded.Volume / count));
            meatComp.CombBlooded = new Solution();
        }

        _solutions.ForceAddSolution(combSolution.Value, new Solution(meatComp.CombHealReagent, meatComp.CombHeal));
    }

    /// <summary>
    /// One kind of meat, by weight for what this colony has brought. Null if nothing in the pool can fall.
    /// </summary>
    public BeeMeatPrototype? PickMeat(MeatBeehiveComponent meatComp)
    {
        var weights = new Dictionary<BeeMeatPrototype, float>();
        foreach (var meat in _proto.EnumeratePrototypes<BeeMeatPrototype>())
        {
            var weight = meat.WeightFor(meatComp.BloodTypes);
            if (weight > 0f)
                weights[meat] = weight;
        }

        return weights.Count > 0 ? _random.Pick(weights) : null;
    }

    /// <summary>
    /// How many pieces of meat fall out: a base amount and one more for every so many bites on the dead, up to a cap.
    /// </summary>
    public static int MeatDrops(MeatBeehiveComponent meatComp)
    {
        return Math.Clamp(meatComp.MeatBase + meatComp.Meat / Math.Max(meatComp.MeatPerDrop, 1), 0, meatComp.MeatDropCap);
    }

    /// <summary>
    /// The meat of a broken meat hive falls out next to it.
    /// </summary>
    private void DropMeat(Entity<BeehiveComponent> hive)
    {
        var meatComp = MeatOf(hive);
        for (var i = 0; i < MeatDrops(meatComp); i++)
        {
            if (PickMeat(meatComp) is not { } meat)
                return;

            foreach (var entity in meat.Entities)
            {
                SpawnNextToOrDrop(entity, hive);
            }
        }
    }

    /// <summary>
    /// The combs of a broken meat hive carry the blood it gathered and the reagent that heals what blood does not.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWildCombsDropped(ref WildCombsDroppedEvent args)
    {
        if (TryComp<BeehiveComponent>(args.Hive, out var hive) && hive.Diet == BeehiveDiet.Meat)
            FillMeatCombs((args.Hive, hive), args.Combs, args.Count);
    }

    /// <summary>
    /// A broken meat hive drops its meat.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBeehiveBroken(ref BeehiveBrokenEvent args)
    {
        if (TryComp<BeehiveComponent>(args.Hive, out var hive) && hive.Wild && hive.Diet == BeehiveDiet.Meat)
            DropMeat((args.Hive, hive));
    }
}
