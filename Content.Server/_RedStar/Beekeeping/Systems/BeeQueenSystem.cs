using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Prototypes;
using Content.Shared._RedStar.Beekeeping.Systems;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// The queen. Her traits and their instability (whatever lands in her solution is sorted out on the next tick:
/// unstable mutagen rolls traits, the rest is payload for the sting), the egg she lays and its royal jelly, and a queen
/// loose outside a hive looking for a home. The hive's own life is <see cref="BeehiveSystem"/>; it asks this system
/// about the queen.
/// </summary>
public sealed partial class BeeQueenSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private BeeScoutSystem _scout = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private BeeErrandSystem _errands = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private BeehiveSystem _hive = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateHoming(frameTime);
        UpdateQueens();
        UpdateQueenLife();
    }

    /// <summary>
    /// Queens whose solution changed. Sorted out in the update, not inside the change event itself.
    /// </summary>
    private readonly HashSet<EntityUid> _changedQueens = new();

    private readonly Dictionary<(ProtoId<BeeTraitPrototype> Trait, bool Gain), float> _rollOutcomes = new();

    [SubscribeLocalEvent]
    private void OnQueenMapInit(Entity<BeeQueenComponent> ent, ref MapInitEvent args)
    {
        UpdateQueenVisuals(ent);
    }

    [SubscribeLocalEvent]
    private void OnQueenSolutionChanged(Entity<BeeQueenComponent> ent, ref SolutionChangedEvent args)
    {
        _changedQueens.Add(ent);
    }

    private void UpdateQueens()
    {
        if (_changedQueens.Count == 0)
            return;

        foreach (var uid in _changedQueens)
        {
            if (!TerminatingOrDeleted(uid) && TryComp<BeeQueenComponent>(uid, out var queen))
                SortQueenSolution((uid, queen));
        }

        _changedQueens.Clear();
    }

    /// <summary>
    /// Once a second: a queen outside a hive lives only so long. A hive, a swarm carrying her or an angry queen
    /// guarding her keeps her alive; anywhere else her clock runs and she dies when it runs out.
    /// </summary>
    private TimeSpan _nextQueenLife;

    private void UpdateQueenLife()
    {
        var now = _timing.CurTime;
        if (now < _nextQueenLife)
            return;

        _nextQueenLife = now + TimeSpan.FromSeconds(1);

        var query = EntityQueryEnumerator<BeeQueenComponent>();
        while (query.MoveNext(out var uid, out var queen))
        {
            var inContainer = _container.TryGetContainingContainer(uid, out var container);
            var home = inContainer && HasComp<BeehiveComponent>(container!.Owner);

            if (home)
            {
                if (queen.DiesAt != null)
                {
                    queen.DiesAt = null;
                    Dirty(uid, queen);
                }

                continue;
            }

            if (queen.DiesAt is not { } dies)
            {
                queen.DiesAt = now + queen.Lifetime;
                Dirty(uid, queen);
                continue;
            }

            if (now < dies)
                continue;

            _popup.PopupCoordinates(Loc.GetString("beekeeping-queen-died"), Transform(uid).Coordinates, PopupType.SmallCaution);
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(uid):queen} died outside a hive");
            QueueDel(uid);
        }
    }

    private void SortQueenSolution(Entity<BeeQueenComponent> queen)
    {
        if (!_solutions.TryGetSolution(queen.Owner, queen.Comp.Solution, out var soln, out var solution))
            return;

        var comp = queen.Comp;
        var mutagen = solution.GetTotalPrototypeQuantity(comp.Mutagen);
        if (mutagen > 0)
        {
            _solutions.RemoveReagent(soln.Value, comp.Mutagen.Id, mutagen);
            comp.MutagenProgress += mutagen;
            while (comp.MutagenPerRoll > 0 && comp.MutagenProgress >= comp.MutagenPerRoll)
            {
                comp.MutagenProgress -= comp.MutagenPerRoll;
                RollTrait(queen);
            }
        }

        var jelly = solution.GetTotalPrototypeQuantity(comp.Jelly);
        if (jelly > 0)
        {
            _solutions.RemoveReagent(soln.Value, comp.Jelly.Id, jelly);
            comp.JellyProgress += jelly;
            while (comp.JellyPerSlot > 0 && comp.JellyProgress >= comp.JellyPerSlot)
            {
                comp.JellyProgress -= comp.JellyPerSlot;
                AddTraitSlot(queen);
            }
        }

        // The traits may have just changed: a queen who lost her sting drops her payload.
        if (!_hive.GetTotals(comp.Traits).Sting)
        {
            if (solution.Volume > 0)
            {
                _solutions.RemoveAllSolution(soln.Value);
                _popup.PopupEntity(Loc.GetString("beekeeping-queen-payload-lost"), queen);
            }
        }
        else if (solution.Volume > comp.PayloadCap)
        {
            _solutions.SplitSolution(soln.Value, solution.Volume - comp.PayloadCap);
        }

        UpdateQueenVisuals(queen);
    }

    /// <summary>
    /// One mutagen roll over the whole pool: gain a trait she lacks or lose one she has. A new trait with no free
    /// slot takes the place of a random slotted one.
    /// </summary>
    private void RollTrait(Entity<BeeQueenComponent> queen)
    {
        var comp = queen.Comp;
        // Every roll wears the queen down a little more, for the rolls after this one.
        comp.Instability++;
        Dirty(queen);
        var instability = comp.Instability - 1;
        _rollOutcomes.Clear();
        foreach (var trait in _proto.EnumeratePrototypes<BeeTraitPrototype>())
        {
            var has = comp.Traits.Contains(trait.ID);
            var weight = SharedBeehiveSystem.RollWeight(trait, has, instability);
            if (weight > 0f)
                _rollOutcomes[(trait.ID, !has)] = weight;
        }

        if (_rollOutcomes.Count == 0)
            return;

        var (picked, gain) = _random.Pick(_rollOutcomes);

        var name = Loc.GetString(_proto.Index(picked).Name);
        string message;
        if (!gain)
        {
            comp.Traits.Remove(picked);
            message = Loc.GetString("beekeeping-queen-trait-lost", ("trait", name));
        }
        else if (_proto.Index(picked).Innate || _hive.SlottedTraits(comp) < comp.Slots)
        {
            comp.Traits.Add(picked);
            message = Loc.GetString("beekeeping-queen-trait-gained", ("trait", name));
        }
        else if (comp.Slots > 0 && RandomSlottedTrait(comp) is { } replaced)
        {
            comp.Traits.Remove(replaced);
            comp.Traits.Add(picked);
            message = Loc.GetString("beekeeping-queen-trait-replaced",
                ("trait", name), ("old", Loc.GetString(_proto.Index(replaced).Name)));
        }
        else
        {
            message = Loc.GetString("beekeeping-queen-trait-nothing");
        }

        Dirty(queen);
        _popup.PopupEntity(message, queen);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(queen):queen} rolled a bee trait: {(gain ? "gained" : "lost")} {picked}, traits now {string.Join(", ", comp.Traits)}");
    }

    /// <summary>
    /// Royal jelly gives the queen room for one more trait, up to her maximum.
    /// </summary>
    private void AddTraitSlot(Entity<BeeQueenComponent> queen)
    {
        if (queen.Comp.Slots >= queen.Comp.MaxSlots)
        {
            _popup.PopupEntity(Loc.GetString("beekeeping-queen-slots-max"), queen);
            return;
        }

        queen.Comp.Slots++;
        Dirty(queen);
        _popup.PopupEntity(Loc.GetString("beekeeping-queen-slot-added", ("slots", queen.Comp.Slots)), queen);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(queen):queen} got a trait slot from royal jelly, now {queen.Comp.Slots}");
    }

    /// <summary>
    /// While a queen is in the hive the colony remembers her traits. Without one, a colony that remembers an
    /// emergency-queen trait raises a new queen after a while, from enough bees and nectar. The new queen is a plain
    /// one: she does not inherit the traits.
    /// </summary>
    public void UpdateColonyTraits(Entity<BeehiveComponent> hive, bool hasQueen)
    {
        var comp = hive.Comp;
        if (hasQueen)
        {
            comp.EmergencyQueenSince = null;
            if (_hive.GetQueen(hive) is { } queenUid && TryComp<BeeQueenComponent>(queenUid, out var queen))
            {
                comp.ColonyTraits.Clear();
                comp.ColonyTraits.AddRange(queen.Traits);
            }

            return;
        }

        var ready = _hive.GetTotals(comp.ColonyTraits).EmergencyQueen &&
                    comp.Pause == BeehivePauseReason.None &&
                    comp.Bees >= comp.EmergencyQueenBees &&
                    comp.Nectar >= comp.EmergencyQueenNectar;
        if (!ready)
        {
            comp.EmergencyQueenSince = null;
            return;
        }

        var now = _timing.CurTime;
        comp.EmergencyQueenSince ??= now;
        if (now - comp.EmergencyQueenSince < comp.EmergencyQueenTime)
            return;

        comp.EmergencyQueenSince = null;
        var queenNew = Spawn(comp.DaughterQueenPrototype, Transform(hive).Coordinates);
        if (!_itemSlots.TryInsert(hive.Owner, comp.QueenSlot, queenNew, null))
        {
            Del(queenNew);
            return;
        }

        comp.Nectar -= comp.EmergencyQueenNectar;
        comp.Bees--;
        comp.ColonyTraits.Clear();
        _popup.PopupEntity(Loc.GetString("beekeeping-hive-emergency-queen"), hive);
    }

    private ProtoId<BeeTraitPrototype>? RandomSlottedTrait(BeeQueenComponent comp)
    {
        var slotted = new List<ProtoId<BeeTraitPrototype>>();
        foreach (var id in comp.Traits)
        {
            if (_proto.Resolve(id, out var trait) && !trait.Innate)
                slotted.Add(id);
        }

        if (slotted.Count == 0)
            return null;

        return _random.Pick(slotted);
    }

    /// <summary>
    /// The queen's look: payload colour on her grey mask, angry eyes for aggressive traits. The hidden bee icons of
    /// her hive follow her.
    /// </summary>
    public void UpdateQueenVisuals(Entity<BeeQueenComponent> queen)
    {
        var (color, angry) = QueenLook(queen);
        SetTraitVisuals(queen, color, angry);

        if (!_container.TryGetContainingContainer(queen.Owner, out var container) ||
            !TryComp<BeehiveComponent>(container.Owner, out var hive))
        {
            return;
        }

        if (_hive.GetIcon(container.Owner, hive.ScoutIconContainer) is { } scoutIcon)
            SetTraitVisuals(scoutIcon, color, angry);
        if (_hive.GetIcon(container.Owner, hive.PollinatorIconContainer) is { } pollinatorIcon)
            SetTraitVisuals(pollinatorIcon, color, angry);
    }

    public (Color? Color, bool Angry) QueenLook(Entity<BeeQueenComponent> queen)
    {
        Color? color = null;
        if (_solutions.TryGetSolution(queen.Owner, queen.Comp.Solution, out _, out var solution) &&
            solution.Volume > 0)
        {
            color = solution.GetColor(_proto);
        }

        return (color, _hive.GetTotals(queen.Comp.Traits).Aggressive);
    }

    /// <summary>
    /// Sets the trait look of a queen or bee sprite. A null colour means the honey colour.
    /// </summary>
    public void SetTraitVisuals(EntityUid uid, Color? color, bool angry)
    {
        if (!TryComp<BeeTraitVisualsComponent>(uid, out var visuals))
            return;

        _appearance.SetData(uid, BeeTraitVisuals.GreyColor, color ?? visuals.HoneyColor);
        _appearance.SetData(uid, BeeTraitVisuals.Angry, angry);
    }

    /// <summary>
    /// A bee taken out of the hive carries the colony's traits and the queen's look until it is back.
    /// </summary>
    public void GiveTraits(Entity<BeehiveComponent> hive, Entity<BeeItemComponent> bee)
    {
        if (_hive.GetQueen(hive) is not { } queenUid || !TryComp<BeeQueenComponent>(queenUid, out var queen))
        {
            bee.Comp.PestControl = false;
            Dirty(bee);
            return;
        }

        var totals = _hive.GetTotals(queen.Traits);
        bee.Comp.Traits = new List<ProtoId<BeeTraitPrototype>>(queen.Traits);
        bee.Comp.PestControl = totals.PestControl;
        bee.Comp.DoubleYieldChance = Math.Clamp(bee.Comp.DoubleYieldChance * totals.PollinationChance, 0f, 1f);
        if (bee.Comp.Role == BeeRole.Pollinator)
            bee.Comp.Charges = Math.Max(1, bee.Comp.Charges + totals.PollinatorCharges);
        bee.Comp.PollinationMutation = totals.PollinationMutation;

        // A long-lived colony's bees last longer outside.
        if (!MathHelper.CloseTo(totals.Lifetime, 1f) && TryComp<TimedDespawnComponent>(bee, out var despawn))
        {
            despawn.Lifetime *= totals.Lifetime;
            bee.Comp.ExpiresAt = _timing.CurTime + TimeSpan.FromSeconds(despawn.Lifetime);
        }
        Dirty(bee);

        // With a sting, the bee carries some of the queen's payload; its colour shows the payload it took.
        Color? color = null;
        if (totals.Sting &&
            _solutions.TryGetSolution(queenUid, queen.Solution, out var soln, out var solution) &&
            solution.Volume > 0)
        {
            var taken = _solutions.SplitSolution(soln.Value,
                FixedPoint2.Min(FixedPoint2.New(hive.Comp.PayloadPerBee), solution.Volume));
            bee.Comp.Payload.AddSolution(taken, _proto);
            color = taken.GetColor(_proto);
            UpdateQueenVisuals((queenUid, queen));
        }

        SetTraitVisuals(bee, color, totals.Aggressive);
    }

    /// <summary>
    /// A new comb takes some of the queen's payload; it goes into the honeycombs when the frame is cut.
    /// </summary>
    public void TakeCombPayload(Entity<BeehiveComponent> hive, BeeFrameComponent? frame)
    {
        if (_hive.GetQueen(hive) is not { } queenUid ||
            !TryComp<BeeQueenComponent>(queenUid, out var queen) ||
            !_solutions.TryGetSolution(queenUid, queen.Solution, out var soln, out var solution) ||
            solution.Volume <= 0)
        {
            return;
        }

        var taken = _solutions.SplitSolution(soln.Value, FixedPoint2.Min(queen.PayloadPerComb, solution.Volume));
        frame?.Payload.AddSolution(taken, _proto);
        UpdateQueenVisuals((queenUid, queen));
    }
}
