using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Events;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Beekeeping.Systems;

/// <summary>
/// Traits: what they add up to, the examine lines of a queen and a bee, and the check that refuses payload for a
/// queen without a sting before the syringe gives anything away.
/// </summary>
public abstract partial class SharedBeehiveSystem
{
    [Dependency] protected IPrototypeManager Proto = default!;
    [Dependency] protected SharedSolutionContainerSystem Solutions = default!;
    [Dependency] private SharedPopupSystem _popupShared = default!;

    /// <summary>
    /// The weight of one outcome of a mutagen roll on a queen with the given instability. Gaining a bad trait gets
    /// likelier (x1 + 0.2 per point), gaining a good one rarer (x1 - 0.1 per point, at least x0.3), losing a good
    /// trait likelier (x1 + 0.2 per point). Losing a bad one is unchanged.
    /// </summary>
    public static float RollWeight(BeeTraitPrototype trait, bool has, int instability)
    {
        if (has)
            return trait.LossWeight * (trait.Bad ? 1f : 1f + 0.2f * instability);

        return trait.Weight * (trait.Bad ? 1f + 0.2f * instability : Math.Max(0.3f, 1f - 0.1f * instability));
    }

    /// <summary>
    /// What a set of traits adds up to.
    /// </summary>
    public BeeTraitTotals GetTotals(IEnumerable<ProtoId<BeeTraitPrototype>> traits)
    {
        var totals = new BeeTraitTotals();
        foreach (var id in traits)
        {
            if (!Proto.Resolve(id, out var trait))
                continue;

            totals.CombRate *= trait.CombRate;
            totals.CombCost *= trait.CombCost;
            totals.NectarPerVisit *= trait.NectarPerVisit;
            totals.PollinationChance *= trait.PollinationChance;
            totals.PollinatorCharges += trait.PollinatorCharges;
            totals.BeeCost *= trait.BeeCost;
            totals.LayRate *= trait.LayRate;
            totals.EggWait *= trait.EggWait;
            totals.EggLife *= trait.EggLife;
            totals.EggCooldown *= trait.EggCooldown;
            totals.Lifetime *= trait.Lifetime;
            totals.ScoutPause *= trait.ScoutPause;
            totals.ScoutFinds += trait.ScoutFinds;
            totals.PollinationMutation += trait.PollinationMutation;
            totals.EmergencyQueen |= trait.EmergencyQueen;
            totals.PestControl |= trait.PestControl;
            totals.Sting |= trait.Sting;
            totals.Aggressive |= trait.Aggressive;
            totals.AngryPerHit += trait.AngryPerHit;
            totals.AngryDamageStep *= trait.AngryDamageStep;
            totals.GuardedScout |= trait.GuardedScout;
        }

        return totals;
    }

    /// <summary>
    /// The traits of the queen in the hive, or none without a queen.
    /// </summary>
    public BeeTraitTotals GetHiveTotals(Entity<BeehiveComponent> hive)
    {
        return GetQueen(hive) is { } queen && TryComp<BeeQueenComponent>(queen, out var comp)
            ? GetTotals(comp.Traits)
            : new BeeTraitTotals();
    }

    /// <summary>
    /// Traits that take a slot.
    /// </summary>
    public int SlottedTraits(BeeQueenComponent queen)
    {
        var count = 0;
        foreach (var id in queen.Traits)
        {
            if (Proto.Resolve(id, out var trait) && !trait.Innate)
                count++;
        }

        return count;
    }

    /// <summary>
    /// "Name, Name" with each name in its colour, or a "none" line.
    /// </summary>
    protected string TraitList(IReadOnlyCollection<ProtoId<BeeTraitPrototype>> traits)
    {
        if (traits.Count == 0)
            return Loc.GetString("beekeeping-traits-none");

        return string.Join(", ", traits.Select(id => Proto.Resolve(id, out var trait)
            ? $"[color={trait.Color.ToHex()}]{Loc.GetString(trait.Name)}[/color]"
            : id.Id));
    }

    [SubscribeLocalEvent]
    private void OnQueenExamined(Entity<BeeQueenComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(BeeQueenComponent)))
        {
            args.PushMarkup(Loc.GetString("beekeeping-queen-examine-traits", ("traits", TraitList(ent.Comp.Traits))));
            args.PushMarkup(Loc.GetString("beekeeping-queen-examine-slots",
                ("used", SlottedTraits(ent.Comp)), ("slots", ent.Comp.Slots)));

            if (ent.Comp.DiesAt is { } dies && dies - Timing.CurTime <= ent.Comp.DyingWarning)
                args.PushMarkup(Loc.GetString("beekeeping-queen-examine-dying"));

            if (Solutions.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution) &&
                solution.Volume > 0)
            {
                args.PushMarkup(Loc.GetString("beekeeping-queen-examine-payload",
                    ("amount", solution.Volume), ("cap", ent.Comp.PayloadCap)));
            }
        }
    }

    /// <summary>
    /// A queen without a sting has nothing to carry a payload in: a syringe with anything but mutagen or royal jelly
    /// is refused and keeps its contents.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnQueenBeforeInject(Entity<BeeQueenComponent> ent, ref TargetBeforeInjectEvent args)
    {
        if (args.Cancelled || GetTotals(ent.Comp.Traits).Sting)
            return;

        if (!TryComp<InjectorComponent>(args.UsedInjector, out var injector) ||
            !Solutions.TryGetSolution(args.UsedInjector, injector.SolutionName, out _, out var contents))
        {
            return;
        }

        foreach (var reagent in contents.Contents)
        {
            if (reagent.Reagent.Prototype == ent.Comp.Mutagen.Id || reagent.Reagent.Prototype == ent.Comp.Jelly.Id)
                continue;

            args.Cancel();
            _popupShared.PopupEntity(Loc.GetString("beekeeping-queen-no-sting"), ent, args.EntityUsingInjector);
            return;
        }
    }
}
