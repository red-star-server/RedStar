using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Data;
using Content.Server._RedStar.Beekeeping.Events;
using Content.Shared.Damage.Systems;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// The foragers of a meat colony can be hit like any small mob. Each one killed costs the colony a bee (the errand
/// takes care of that) and makes it furious for a while: the fewer bees are left, the faster they fly, the more are out
/// at once and the likelier they go after whoever did it (a couple of times only, and never with the last few bees
/// left).
/// </summary>
public sealed partial class MeatBeehiveSystem
{
    /// <summary>
    /// How furious the colony is, from 0 (calm) to 1: at least <see cref="BeehiveComponent.RageMin"/> once provoked,
    /// growing as bees are lost.
    /// </summary>
    public float RageLevel(Entity<BeehiveComponent> hive)
    {
        var comp = hive.Comp;
        if (comp.Diet != BeehiveDiet.Meat ||
            !TryComp<MeatBeehiveComponent>(hive, out var meatComp) ||
            _timing.CurTime >= meatComp.RageUntil)
        {
            return 0f;
        }

        var left = comp.MaxBees > 0 ? (comp.Bees + comp.OutBees) / (float) comp.MaxBees : 0f;
        return Math.Clamp(Math.Max(meatComp.RageMin, 1f - left), 0f, 1f);
    }

    /// <summary>
    /// Whoever hit a forager is remembered, whether the bee dies or not.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnForagerHurt(Entity<BeeHunterComponent> bee, ref DamageDealtEvent args)
    {
        if (!args.Damage.AnyPositive() ||
            args.Origin is not { } attacker ||
            !TryComp<BeeErrandComponent>(bee, out var errand) ||
            TerminatingOrDeleted(errand.Hive) ||
            !TryComp<BeehiveComponent>(errand.Hive, out var hive) ||
            hive.Diet != BeehiveDiet.Meat)
        {
            return;
        }

        // Someone already marked keeps what is left of the revenge: hitting more bees does not earn more attacks.
        var meatComp = MeatOf(errand.Hive);
        var now = _timing.CurTime;
        if (meatComp.Attackers.TryGetValue(attacker, out var known) && now < known.Until)
            meatComp.Attackers[attacker] = (now + meatComp.RageDuration, known.Left);
        else
            meatComp.Attackers[attacker] = (now + meatComp.RageDuration, meatComp.RageRevenge);
    }

    /// <summary>
    /// A forager of a meat colony did not come back (the colony already lost the bee): it rages for a while.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnForagerLost(ref BeeErrandLostEvent args)
    {
        if (args.Kind != BeeErrandKind.Hunt ||
            !TryComp<BeehiveComponent>(args.Hive, out var comp) ||
            comp.Diet != BeehiveDiet.Meat)
        {
            return;
        }

        var meatComp = MeatOf(args.Hive);
        meatComp.RageUntil = _timing.CurTime + meatComp.RageDuration;
    }

    /// <summary>
    /// How much likelier a creature is to be the next target: it killed a bee lately and the colony remembers.
    /// </summary>
    private float AttackerBias(Entity<BeehiveComponent> hive, EntityUid target, float rage, TimeSpan now)
    {
        var comp = hive.Comp;
        var meatComp = MeatOf(hive);
        // The last few bees do not risk themselves for revenge.
        return rage > 0f &&
               comp.Bees + comp.OutBees > meatComp.RageRevengeMinBees &&
               meatComp.Attackers.TryGetValue(target, out var mark) && now < mark.Until && mark.Left > 0
            ? 1f + (meatComp.RageAttackerBias - 1f) * rage
            : 1f;
    }

    /// <summary>
    /// A bite is sent at a creature the colony wants revenge on: one of its few attacks is used up.
    /// </summary>
    private void UseRevenge(Entity<BeehiveComponent> hive, EntityUid target)
    {
        var meatComp = MeatOf(hive);
        if (meatComp.Attackers.TryGetValue(target, out var mark) && mark.Left > 0)
            meatComp.Attackers[target] = (mark.Until, mark.Left - 1);
    }
}
