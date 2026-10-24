using System.Linq;
using Content.Server.Destructible;
using Content.Shared.Botany.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Humanoid;
using Content.Shared.Damage.Systems;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Prototypes;
using Content.Shared._RedStar.Beekeeping.Systems;
using Content.Shared._RedStar.Beekeeping.UI;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// Fills the apiary analyzer's window from what the hive, queen or bee knows, and refreshes open windows once a
/// second.
/// </summary>
public sealed partial class ApiaryAnalyzerSystem : SharedApiaryAnalyzerSystem
{
    [Dependency] private MeatBeehiveSystem _meat = default!;
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [Dependency] private BeehiveSystem _beehive = default!;
    [Dependency] private BeeScoutSystem _scout = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DestructibleSystem _destructible = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;

    private readonly List<Entity<BeeFrameComponent>> _frames = new();
    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<ApiaryAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Target != null)
                UpdateUi((uid, comp));
        }
    }

    protected override void UpdateUi(Entity<ApiaryAnalyzerComponent> ent)
    {
        if (ent.Comp.Target is { } target &&
            UI.IsUiOpen(ent.Owner, ApiaryAnalyzerUiKey.Key) &&
            BuildState(target) is { } state)
        {
            UI.SetUiState(ent.Owner, ApiaryAnalyzerUiKey.Key, state);
        }
    }

    /// <summary>
    /// What the window shows for a hive, a queen or a bee; null for anything else or something already gone.
    /// </summary>
    public ApiaryAnalyzerState? BuildState(EntityUid target)
    {
        if (TerminatingOrDeleted(target))
            return null;

        var state = new ApiaryAnalyzerState { Target = GetNetEntity(target) };

        if (TryComp<BeehiveComponent>(target, out var hive))
        {
            state.Kind = ApiaryScanKind.Hive;
            FillHive((target, hive), state);
        }
        else if (TryComp<BeeQueenComponent>(target, out var queen))
        {
            state.Kind = ApiaryScanKind.Queen;
            FillQueen((target, queen), state);
        }
        else if (TryComp<BeeItemComponent>(target, out var bee))
        {
            state.Kind = bee.Role == BeeRole.Scout ? ApiaryScanKind.Scout : ApiaryScanKind.Pollinator;
            state.Traits.AddRange(bee.Traits);
            state.Totals = _beehive.GetTotals(bee.Traits);
            state.Charges = bee.Charges;
            state.HiveKnown = bee.SourceHive is { } home && !TerminatingOrDeleted(home);
            state.Memory = bee.MemoryCount;
            state.MemoryLimit = bee.MemoryLimit;
        }
        else
        {
            return null;
        }

        return state;
    }

    private void FillQueen(Entity<BeeQueenComponent> queen, ApiaryAnalyzerState state)
    {
        state.HasQueen = true;
        state.Traits.AddRange(queen.Comp.Traits);
        state.Totals = _beehive.GetTotals(queen.Comp.Traits);
        state.UsedSlots = _beehive.SlottedTraits(queen.Comp);
        state.Instability = queen.Comp.Instability;
        state.Slots = queen.Comp.Slots;
        state.PayloadCap = queen.Comp.PayloadCap.Float();
        if (_solutions.TryGetSolution(queen.Owner, queen.Comp.Solution, out _, out var payload))
            state.Payload = payload.Volume.Float();
    }

    private void FillHive(Entity<BeehiveComponent> hive, ApiaryAnalyzerState state)
    {
        var comp = hive.Comp;
        state.Wild = comp.Wild;
        state.Diet = comp.Diet;
        if (comp.Diet == BeehiveDiet.Meat)
            FillMeat(hive, state);
        state.Bees = comp.Bees;
        state.OutBees = comp.OutBees;
        state.MaxBees = comp.MaxBees;
        state.Nectar = comp.Nectar;
        state.MaxNectar = comp.MaxNectar;
        state.HasEgg = comp.HasEgg;
        state.NectarUse = _beehive.GetNectarUse(hive);
        state.BroodGoing = _beehive.IsBroodGoing(hive);
        // Bees back home are gone from the world but stay in the list until the next take-off clears it.
        state.Foraging = comp.Foragers.Count(bee => !TerminatingOrDeleted(bee));

        if (_beehive.GetQueen(hive) is { } queenUid && TryComp<BeeQueenComponent>(queenUid, out var queen))
            FillQueen((queenUid, queen), state);

        if (comp.Wild)
        {
            state.Combs = comp.WildCombs;
            state.MaxCombs = comp.MaxWildCombs;
        }
        else
        {
            _beehive.GetFrames(hive, _frames);
            foreach (var frame in _frames)
            {
                state.Combs += frame.Comp.Combs;
                state.MaxCombs += frame.Comp.MaxCombs;
                state.Frames.Add(new ApiaryFrameData
                {
                    Frame = GetNetEntity(frame),
                    Prototype = MetaData(frame).EntityPrototype?.ID,
                    Combs = frame.Comp.Combs,
                    MaxCombs = frame.Comp.MaxCombs,
                });
            }
        }

        foreach (var source in comp.KnownSources)
        {
            if (TerminatingOrDeleted(source.Tray))
                continue;

            if (comp.Diet == BeehiveDiet.Meat)
            {
                // Only the kind of creature, never its name or what it wears: a humanoid by species, anything else by what
                // it is.
                var data = new ApiaryTrayData
                {
                    State = _scout.GetTrayState(hive, source),
                    Creature = true,
                    CreaturePrototype = MetaData(source.Tray).EntityPrototype?.ID,
                };

                if (TryComp<HumanoidProfileComponent>(source.Tray, out var profile) &&
                    _proto.TryIndex(profile.Species, out var species))
                {
                    data.CreatureName = species.Name;
                    data.CreaturePrototype = species.Prototype;
                }

                state.Trays.Add(data);
                continue;
            }

            var plant = TryComp<PlantTrayComponent>(source.Tray, out var tray) &&
                        tray.PlantEntity is { } plantUid &&
                        !TerminatingOrDeleted(plantUid)
                ? plantUid
                : (EntityUid?) null;

            state.Trays.Add(new ApiaryTrayData
            {
                Tray = GetNetEntity(source.Tray),
                Plant = plant is { } p ? GetNetEntity(p) : null,
                PlantPrototype = plant is { } named ? MetaData(named).EntityPrototype?.ID : null,
                State = _scout.GetTrayState(hive, source),
            });
        }

        if (TryComp<DamageableComponent>(hive, out var damageable))
        {
            var max = _destructible.DestroyedAt(hive).Float();
            state.MaxHealth = max;
#pragma warning disable CS0618
            state.Health = Math.Max(0f, max - _damageable.GetTotalDamage((hive, damageable)).Float());
#pragma warning restore CS0618
        }
    }

    /// <summary>
    /// What a meat hive would give when broken now, and what it holds: the blood in stock and in the combs, and the
    /// healing reagent the combs will carry.
    /// </summary>
    private void FillMeat(Entity<BeehiveComponent> hive, ApiaryAnalyzerState state)
    {
        var meatComp = _meat.MeatOf(hive);
        var comp = hive.Comp;
        state.MeatDrops = MeatBeehiveSystem.MeatDrops(meatComp);
        state.MeatMax = meatComp.MeatDropCap;
        state.MeatBase = meatComp.MeatBase;
        state.MeatPerDrop = meatComp.MeatPerDrop;

        var total = 0f;
        foreach (var meat in _proto.EnumeratePrototypes<BeeMeatPrototype>())
        {
            total += meat.WeightFor(meatComp.BloodTypes);
        }

        if (total > 0f)
        {
            foreach (var meat in _proto.EnumeratePrototypes<BeeMeatPrototype>())
            {
                var weight = meat.WeightFor(meatComp.BloodTypes);
                if (weight > 0f)
                    state.MeatChances.Add(new ApiaryMeatChance { Meat = meat.ID, Percent = weight / total * 100f });
            }

            state.MeatChances.Sort((a, b) => b.Percent.CompareTo(a.Percent));
        }

        var held = new Dictionary<string, float>();
        void Add(Solution solution)
        {
            foreach (var (reagent, quantity) in solution.Contents)
            {
                held[reagent.Prototype] = held.GetValueOrDefault(reagent.Prototype) + quantity.Float();
            }
        }

        Add(meatComp.Blood);
        Add(meatComp.CombBlooded);
        if (comp.WildCombs > 0)
            held[meatComp.CombHealReagent] = held.GetValueOrDefault(meatComp.CombHealReagent) + meatComp.CombHeal * comp.WildCombs;

        foreach (var (reagent, amount) in held)
        {
            if (amount > 0f)
                state.Chemicals.Add(new ApiaryReagentData { Reagent = reagent, Amount = amount });
        }
    }
}
