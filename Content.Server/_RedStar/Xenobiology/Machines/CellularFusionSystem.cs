using System.Linq;
using Content.Server._RedStar.Xenobiology.Events;
using Content.Server.Power.EntitySystems;
using Content.Shared.Research.Components;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Systems;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.GameTicking;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Containers;

namespace Content.Server._RedStar.Xenobiology.Machines;

public sealed partial class CellularFusionSystem : EntitySystem
{
    [Dependency] private XenobiologyDatabaseSystem _database = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private SharedCellSystem _cell = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IGameTiming _timing = default!;

    private Dictionary<string, ProtoId<CellTraitPrototype>>? _mutationRecipes;

    [SubscribeLocalEvent]
    private void OnConnectionChanged(Entity<CellularFusionComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateConnection(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CellularFusionComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            CancelSplice(ent);
    }

    [SubscribeLocalEvent]
    private void OnDishRemoved(Entity<CellularFusionComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.DishSlot)
            CancelSplice(ent);
    }

    [SubscribeLocalEvent]
    private void OnDestruction(Entity<CellularFusionComponent> ent, ref DestructionEventArgs args)
    {
        CancelSplice(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CellularFusionComponent> ent, ref ComponentShutdown args)
    {
        CancelSplice(ent);
    }

    private void UpdateConnection(Entity<CellularFusionComponent> ent)
    {
        if (!_database.TryGetDatabase(ent.Owner, out _))
            CancelSplice(ent);

        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        _mutationRecipes = null;
    }

    private void EnsureRecipesInitialized()
    {
        if (_mutationRecipes is not null)
            return;

        _mutationRecipes = new Dictionary<string, ProtoId<CellTraitPrototype>>();
        var catalog = _prototypes.EnumeratePrototypes<CellTraitPrototype>()
            .OrderBy(prototype => prototype.ID, StringComparer.Ordinal)
            .ToArray();
        var available = catalog
            .Where(prototype => prototype is { FusionIngredient: true, FusionMutation: false })
            .Select(prototype => new ProtoId<CellTraitPrototype>(prototype.ID))
            .ToList();

        foreach (var mutation in catalog.Where(prototype => prototype.FusionMutation))
        {
            if (available.Count < 2)
                break;

            var idx1 = _random.Next(available.Count);
            var a = available[idx1];
            available.RemoveAt(idx1);

            var idx2 = _random.Next(available.Count);
            var b = available[idx2];
            available.RemoveAt(idx2);

            var key = GetRecipeKey(a, b);
            _mutationRecipes[key] = mutation.ID;
        }
    }

    private static string GetRecipeKey(ProtoId<CellTraitPrototype> a, ProtoId<CellTraitPrototype> b)
    {
        return string.Compare(a, b, StringComparison.Ordinal) <= 0
            ? $"{a}+{b}"
            : $"{b}+{a}";
    }

    private ProtoId<CellTraitPrototype>? CheckMutationRecipe(List<ProtoId<CellTraitPrototype>> traits)
    {
        EnsureRecipesInitialized();

        for (var i = 0; i < traits.Count; i++)
        {
            for (var j = i + 1; j < traits.Count; j++)
            {
                var key = GetRecipeKey(traits[i], traits[j]);
                if (_mutationRecipes!.TryGetValue(key, out var mutation))
                    return mutation;
            }
        }

        return null;
    }

    [SubscribeLocalEvent]
    private void OnMaterialAmountChanged(Entity<CellularFusionComponent> ent, ref MaterialAmountChangedEvent args)
    {
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnAfterOpen(Entity<CellularFusionComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnServerDatabaseChanged(Entity<CellularFusionComponent> ent, ref XenobiologyDatabaseChangedEvent args)
    {
        UpdateConnection(ent);
    }

    [SubscribeLocalEvent]
    private void OnSpliceMessage(Entity<CellularFusionComponent> ent, ref CellularFusionUiSpliceMessage args)
    {
        if (ent.Comp.SpliceInProgress || !this.IsPowered(ent.Owner, EntityManager))
            return;

        if (!_database.TryGetCell(ent.Owner, args.CellAId, out var cellA) ||
            !_database.TryGetCell(ent.Owner, args.CellBId, out var cellB))
            return;

        var dishUid = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot);
        if (dishUid is not { } dish)
        {
            _popup.PopupEntity(Loc.GetString("cellular-fusion-no-dish"), ent, PopupType.MediumCaution);
            return;
        }

        if (!TryComp<CellContainerComponent>(dish, out _))
            return;

        var cost = SharedCellSystem.GetMergedCost(cellA, cellB);
        var material = _materialStorage.GetMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial);
        if (cost > material)
            return;

        // A started operation keeps its selected genomes even if the database changes.
        ent.Comp.SpliceCellA = cellA;
        ent.Comp.SpliceCellB = cellB;
        ent.Comp.SpliceDish = dish;
        ent.Comp.SpliceCost = cost;
        ent.Comp.SpliceDuration = TimeSpan.FromSeconds(ent.Comp.SpliceDelay);
        ent.Comp.SpliceEndTime = _timing.CurTime + ent.Comp.SpliceDuration;
        UpdateUI(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CellularFusionComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var component, out var metadata))
        {
            if (metadata.EntityPaused || component.SpliceEndTime is not { } endTime)
                continue;

            var machine = new Entity<CellularFusionComponent>(uid, component);
            if (!this.IsPowered(uid, EntityManager) ||
                !_database.TryGetDatabase(uid, out _) ||
                component.SpliceDish != _itemSlots.GetItemOrNull(uid, component.DishSlot))
            {
                CancelSplice(machine);
                continue;
            }

            if (_timing.CurTime >= endTime)
                FinishSplice(machine);
        }
    }

    private void FinishSplice(Entity<CellularFusionComponent> ent)
    {
        var cellA = ent.Comp.SpliceCellA;
        var cellB = ent.Comp.SpliceCellB;
        var dish = ent.Comp.SpliceDish;
        var cost = ent.Comp.SpliceCost;

        if (cellA is null || cellB is null || dish is not { } dishUid ||
            _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot) != dishUid ||
            !TryComp<CellContainerComponent>(dishUid, out var dishContainer))
        {
            CancelSplice(ent);
            _popup.PopupEntity(Loc.GetString("cellular-fusion-dish-removed"), ent, PopupType.MediumCaution);
            return;
        }

        var material = _materialStorage.GetMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial);
        if (material < cost ||
            !_materialStorage.TrySetMaterialAmount(ent, ent.Comp.RequiredMaterial, material - cost))
        {
            CancelSplice(ent);
            return;
        }

        ResetSplice(ent.Comp);

        var avgStability = (cellA.Stability + cellB.Stability) / 2f;
        var failureChance = Math.Clamp(ent.Comp.BaseFailureChance + (1f - avgStability) * ent.Comp.StabilityMultiplier, 0f, 1f);
        if (_random.Prob(failureChance))
        {
            _popup.PopupEntity(Loc.GetString("cellular-fusion-splice-failure"), ent, PopupType.MediumCaution);
            UpdateUI(ent);
            return;
        }

        var traits = InheritTraits(cellA, cellB);
        var mutation = CheckMutationRecipe(traits);
        if (mutation is not null)
        {
            traits.Add(mutation.Value);
        }

        var result = new Cell(
            prototypeId: null,
            color: SharedCellSystem.GetMergedColor(cellA, cellB),
            name: SharedCellSystem.GetMergedName(cellA, cellB),
            stability: SharedCellSystem.GetMergedStability(cellA, cellB),
            cost: SharedCellSystem.GetCellCost(traits.Count),
            traits: traits);

        _cell.AddCell((dishUid, dishContainer), result);
        _popup.PopupEntity(Loc.GetString("cellular-fusion-splice-success"), ent, PopupType.Medium);
        UpdateUI(ent, result);
    }

    private void CancelSplice(Entity<CellularFusionComponent> ent)
    {
        if (!ent.Comp.SpliceInProgress)
            return;

        ResetSplice(ent.Comp);
        UpdateUI(ent);
    }

    private static void ResetSplice(CellularFusionComponent component)
    {
        component.SpliceEndTime = null;
        component.SpliceDuration = TimeSpan.Zero;
        component.SpliceCellA = null;
        component.SpliceCellB = null;
        component.SpliceDish = null;
        component.SpliceCost = 0;
    }
    private List<ProtoId<CellTraitPrototype>> InheritTraits(Cell cellA, Cell cellB)
    {
        var combined = new List<ProtoId<CellTraitPrototype>>();
        var seen = new HashSet<ProtoId<CellTraitPrototype>>();

        foreach (var trait in cellA.Traits)
        {
            if (_random.Prob(cellA.Stability) && seen.Add(trait))
                combined.Add(trait);
        }

        foreach (var trait in cellB.Traits)
        {
            if (_random.Prob(cellB.Stability) && seen.Add(trait))
                combined.Add(trait);
        }

        return combined;
    }

    private void UpdateUI(Entity<CellularFusionComponent> ent, Cell? lastResult = null)
    {
        if (TerminatingOrDeleted(ent))
            return;
        _database.TryGetDatabase(ent.Owner, out var serverEnt);
        var material = _materialStorage.GetMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial);
        var remaining = ent.Comp.SpliceEndTime is { } endTime
            ? Math.Max(0, (endTime - _timing.CurTime).TotalSeconds)
            : 0;
        var state = new CellularFusionUiState(
            serverEnt?.Comp.Cells.ToArray() ?? [],
            Math.Max(0, material - ent.Comp.SpliceCost),
            ent.Comp.SpliceCost,
            ent.Comp.SpliceInProgress,
            ent.Comp.SpliceDuration.TotalSeconds,
            remaining,
            lastResult);
        _userInterface.SetUiState(ent.Owner, CellularFusionUiKey.Key, state);
    }
}
