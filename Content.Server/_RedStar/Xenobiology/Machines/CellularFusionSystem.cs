using System.Linq;
using Content.Server._RedStar.Genetics;
using Content.Shared._RedStar.Genetics;
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
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.UserInterface;
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
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnGetMaterialWhitelist(Entity<CellularFusionComponent> ent, ref GetMaterialWhitelistEvent args)
    {
        args.Whitelist.Add(ent.Comp.RequiredMaterial);
    }

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
        if (!_database.TryGetDatabase(ent.Owner, out var database) ||
            ent.Comp.SpliceInProgress &&
            (ent.Comp.SpliceServer != database.Value.Owner ||
             ent.Comp.SpliceCellAId is not { } firstId ||
             ent.Comp.SpliceCellBId is not { } secondId ||
             !database.Value.Comp.Cells.Exists(entry => entry.Id == firstId) ||
             !database.Value.Comp.Cells.Exists(entry => entry.Id == secondId)))
            CancelSplice(ent);

        UpdateUI(ent);
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

        if (!_database.TryGetDatabase(ent.Owner, out var database))
            return;

        if (args.CellAId == args.CellBId ||
            !_database.TryGetCell(ent.Owner, args.CellAId, out var cellA) ||
            !_database.TryGetCell(ent.Owner, args.CellBId, out var cellB))
            return;

        if (!CanStabilize(cellA, cellB, out _))
        {
            _popup.PopupEntity(Loc.GetString("cellular-fusion-incompatible"), ent, PopupType.MediumCaution);
            return;
        }

        var dishUid = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot);
        if (dishUid is not { } dish)
        {
            _popup.PopupEntity(Loc.GetString("cellular-fusion-no-dish"), ent, PopupType.MediumCaution);
            return;
        }

        if (!TryComp<CellContainerComponent>(dish, out var dishContainer) ||
            dishContainer.Cells.Count >= dishContainer.MaxCells)
            return;

        var cost = SharedCellSystem.StabilizationCost;
        var material = _materialStorage.GetMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial);
        if (cost > material)
            return;

        ent.Comp.SpliceCellAId = args.CellAId;
        ent.Comp.SpliceCellBId = args.CellBId;
        ent.Comp.SpliceDish = dish;
        ent.Comp.SpliceServer = database.Value.Owner;
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
                !_database.TryGetDatabase(uid, out var database) ||
                component.SpliceServer != database.Value.Owner ||
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
        var firstId = ent.Comp.SpliceCellAId;
        var secondId = ent.Comp.SpliceCellBId;
        var dish = ent.Comp.SpliceDish;
        var cost = ent.Comp.SpliceCost;

        if (firstId is not { } idA || secondId is not { } idB || dish is not { } dishUid ||
            !_database.TryGetDatabase(ent.Owner, out var database) ||
            database.Value.Owner != ent.Comp.SpliceServer ||
            _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot) != dishUid ||
            !TryComp<CellContainerComponent>(dishUid, out var dishContainer) ||
            dishContainer.Cells.Count >= dishContainer.MaxCells)
        {
            CancelSplice(ent);
            _popup.PopupEntity(Loc.GetString("cellular-fusion-dish-removed"), ent, PopupType.MediumCaution);
            return;
        }

        if (!_database.TryGetCell(ent.Owner, idA, out var cellA) ||
            !_database.TryGetCell(ent.Owner, idB, out var cellB) ||
            !CanStabilize(cellA, cellB, out var genome))
        {
            CancelSplice(ent);
            return;
        }

        var material = _materialStorage.GetMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial);
        if (material < cost ||
            !_materialStorage.TrySetMaterialAmount(ent, ent.Comp.RequiredMaterial, material - cost))
        {
            CancelSplice(ent);
            return;
        }

        var avgStability = (cellA.Stability + cellB.Stability) / 2f;
        var failureChance = Math.Clamp(ent.Comp.BaseFailureChance + (1f - avgStability) * ent.Comp.StabilityMultiplier, 0f, 1f);
        var failed = _random.Prob(failureChance);
        var result = failed ? null : new Cell(
            prototypeId: null,
            color: SharedCellSystem.GetStabilizedColor(cellA, cellB),
            name: SharedCellSystem.GetStabilizedName(cellA, cellB),
            stability: SharedCellSystem.GetStabilizedStability(cellA, cellB),
            genome: genome);

        ResetSplice(ent.Comp);
        if (!_database.CompletePair(ent.Owner, idA, idB, (dishUid, dishContainer), result))
        {
            _materialStorage.TrySetMaterialAmount(ent, ent.Comp.RequiredMaterial, material);
            UpdateUI(ent);
            return;
        }

        _popup.PopupEntity(Loc.GetString(failed ? "cellular-fusion-splice-failure" : "cellular-fusion-splice-success"), ent, failed ? PopupType.MediumCaution : PopupType.Medium);
        UpdateUI(ent, result);
    }

    private bool CanStabilize(Cell first, Cell second, out GenomeSnapshot? genome)
    {
        genome = null;
        if (!float.IsFinite(first.Stability) || !float.IsFinite(second.Stability) ||
            first.Stability is < 0f or > 1f || second.Stability is < 0f or > 1f ||
            Math.Max(first.Stability, second.Stability) >= 1f ||
            first.GetGenome() is not { } firstGenome || second.GetGenome() is not { } secondGenome)
            return false;

        if (!firstGenome.SameContents(secondGenome) ||
            _genomes.ValidateSnapshot(firstGenome) != GenomeOperationResult.Success)
            return false;

        genome = firstGenome;
        return true;
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
        component.SpliceCellAId = null;
        component.SpliceCellBId = null;
        component.SpliceDish = null;
        component.SpliceServer = null;
        component.SpliceCost = 0;
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
            serverEnt?.Comp.Cells.Select(entry => new CellEntryView(entry.Id, entry.Cell)).ToArray() ?? [],
            Math.Max(0, material - ent.Comp.SpliceCost),
            ent.Comp.SpliceCost,
            ent.Comp.SpliceInProgress,
            ent.Comp.SpliceDuration.TotalSeconds,
            remaining,
            lastResult is null ? null : new CellView(lastResult));
        _userInterface.SetUiState(ent.Owner, CellularFusionUiKey.Key, state);
    }
}
