using System.Linq;
using Content.Server._RedStar.Genetics;
using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.Systems;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology.Machines;

public sealed partial class CellularFusionSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedCellSystem _cells = default!;
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private IGameTiming _timing = default!;

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
        {
            CancelSplice(ent);
            UpdateUI(ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnDishInserted(Entity<CellularFusionComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.DishSlot)
            UpdateUI(ent);
    }

    public void OnDishCellChanged(Entity<CellContainerComponent> ent)
    {
        if (!_containers.TryGetContainingContainer(ent.Owner, out var container) ||
            !TryComp<CellularFusionComponent>(container.Owner, out var fusion) ||
            container.ID != fusion.DishSlot) return;

        if (fusion.SpliceInProgress && fusion.SpliceRevision != ent.Comp.Revision)
            CancelSplice((container.Owner, fusion));
        UpdateUI((container.Owner, fusion));
    }

    [SubscribeLocalEvent]
    private void OnDestruction(Entity<CellularFusionComponent> ent, ref DestructionEventArgs args) => CancelSplice(ent);

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CellularFusionComponent> ent, ref ComponentShutdown args) => CancelSplice(ent);

    [SubscribeLocalEvent]
    private void OnAfterOpen(Entity<CellularFusionComponent> ent, ref AfterActivatableUIOpenEvent args) => UpdateUI(ent);

    [SubscribeLocalEvent]
    private void OnSpliceMessage(Entity<CellularFusionComponent> ent, ref CellularFusionUiSpliceMessage args)
    {
        if (ent.Comp.SpliceInProgress || !this.IsPowered(ent.Owner, EntityManager) ||
            !TryGetDish(ent, out var dish) || args.Dish != GetNetEntity(dish.Owner) ||
            args.DishRevision != dish.Comp.Revision || args.CellAIndex == args.CellBIndex ||
            args.CellAIndex < 0 || args.CellBIndex < 0 ||
            args.CellAIndex >= dish.Comp.Cells.Count || args.CellBIndex >= dish.Comp.Cells.Count)
            return;

        var first = dish.Comp.Cells[args.CellAIndex];
        var second = dish.Comp.Cells[args.CellBIndex];
        if (!CanStabilize(first, second, out _))
        {
            _popup.PopupEntity(Loc.GetString("cellular-fusion-incompatible"), ent, PopupType.MediumCaution);
            return;
        }

        ent.Comp.SpliceCellA = first;
        ent.Comp.SpliceCellB = second;
        ent.Comp.SpliceDish = dish.Owner;
        ent.Comp.SpliceRevision = dish.Comp.Revision;
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
            if (!this.IsPowered(uid, EntityManager) || !TryGetDish(machine, out var dish) ||
                component.SpliceDish != dish.Owner || component.SpliceRevision != dish.Comp.Revision)
            {
                CancelSplice(machine);
                continue;
            }

            if (_timing.CurTime >= endTime)
                FinishSplice(machine, dish);
        }
    }

    private void FinishSplice(Entity<CellularFusionComponent> ent, Entity<CellContainerComponent> dish)
    {
        var first = ent.Comp.SpliceCellA;
        var second = ent.Comp.SpliceCellB;
        if (first is null || second is null || ReferenceEquals(first, second) ||
            !dish.Comp.Cells.Contains(first) || !dish.Comp.Cells.Contains(second) ||
            !CanStabilize(first, second, out var genome))
        {
            CancelSplice(ent);
            return;
        }

        var result = new Cell(null, SharedCellSystem.GetStabilizedColor(first, second),
            SharedCellSystem.GetStabilizedName(first, second),
            SharedCellSystem.GetStabilizedStability(first, second), genome);
        ResetSplice(ent.Comp);
        // The result replaces one source sample before the second is removed, so even a full dish has room.
        if (!_cells.RemoveCell((dish.Owner, (CellContainerComponent?) dish.Comp), first))
            return;
        if (!_cells.RemoveCell((dish.Owner, (CellContainerComponent?) dish.Comp), second))
        {
            _cells.AddCell((dish.Owner, (CellContainerComponent?) dish.Comp), first);
            return;
        }
        _cells.AddCell((dish.Owner, (CellContainerComponent?) dish.Comp), result);
        _popup.PopupEntity(Loc.GetString("cellular-fusion-splice-success"), ent, PopupType.Medium);
        UpdateUI(ent, result);
    }

    private bool CanStabilize(Cell first, Cell second, out GenomeSnapshot? genome)
    {
        genome = null;
        if (!float.IsFinite(first.Stability) || !float.IsFinite(second.Stability) ||
            first.Stability is < 0f or > 1f || second.Stability is < 0f or > 1f ||
            Math.Max(first.Stability, second.Stability) >= 1f ||
            first.GetGenome() is not { } firstGenome || second.GetGenome() is not { } secondGenome ||
            !firstGenome.SameContents(secondGenome) ||
            _genomes.ValidateSnapshot(firstGenome) != GenomeOperationResult.Success)
            return false;

        genome = firstGenome;
        return true;
    }

    private bool TryGetDish(Entity<CellularFusionComponent> ent, out Entity<CellContainerComponent> dish)
    {
        dish = default;
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot) is not { } uid ||
            TerminatingOrDeleted(uid) || !TryComp<CellContainerComponent>(uid, out var container))
            return false;

        dish = (uid, container);
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
        component.SpliceCellA = null;
        component.SpliceCellB = null;
        component.SpliceDish = null;
        component.SpliceRevision = 0;
    }

    private void UpdateUI(Entity<CellularFusionComponent> ent, Cell? lastResult = null)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var hasDish = TryGetDish(ent, out var dish);
        var remaining = ent.Comp.SpliceEndTime is { } endTime
            ? Math.Max(0, (endTime - _timing.CurTime).TotalSeconds)
            : 0;
        _userInterface.SetUiState(ent.Owner, CellularFusionUiKey.Key, new CellularFusionUiState(
            hasDish ? dish.Comp.Cells.Select(cell => new CellView(cell)).ToArray() : [],
            hasDish ? dish.Comp.Revision : -1,
            hasDish ? GetNetEntity(dish.Owner) : null,
            ent.Comp.SpliceInProgress,
            ent.Comp.SpliceDuration.TotalSeconds,
            remaining,
            lastResult is null ? null : new CellView(lastResult)));
    }
}
