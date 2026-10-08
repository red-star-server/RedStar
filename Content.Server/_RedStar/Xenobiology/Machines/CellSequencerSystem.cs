using Content.Server._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.Systems;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;

namespace Content.Server._RedStar.Xenobiology.Machines;

public sealed partial class CellSequencerSystem : EntitySystem
{
    [Dependency] private XenobiologyDatabaseSystem _database = default!;
    [Dependency] private SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private SharedCellSystem _cell = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    [SubscribeLocalEvent]
    private void OnGetMaterialWhitelist(Entity<CellSequencerComponent> ent, ref GetMaterialWhitelistEvent args)
    {
        args.Whitelist.Add(ent.Comp.RequiredMaterial);
    }

    [SubscribeLocalEvent]
    private void OnOpened(Entity<CellSequencerComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnDatabaseChanged(Entity<CellSequencerComponent> ent, ref XenobiologyDatabaseChangedEvent args)
    {
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnConnectionChanged(Entity<CellSequencerComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<CellSequencerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.DishSlot)
        {
            UpdateUI(ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CellSequencerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.DishSlot)
        {
            UpdateUI(ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnCellChanged(Entity<CellContainerComponent> ent, ref CellContainerChangedEvent args)
    {
        UpdateDishOwner(ent.Owner);
    }

    private void UpdateDishOwner(EntityUid dish)
    {
        if (_containers.TryGetContainingContainer(dish, out var container) &&
            TryComp<CellSequencerComponent>(container.Owner, out var sequencer) &&
            container.ID == sequencer.DishSlot)
        {
            UpdateUI((container.Owner, sequencer));
        }
    }

    [SubscribeLocalEvent]
    private void OnMaterialAmountChanged(Entity<CellSequencerComponent> ent, ref MaterialAmountChangedEvent args)
    {
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnAdd(Entity<CellSequencerComponent> ent, ref CellSequencerUiAddMessage args)
    {
        if (!_database.TryGetDatabase(ent.Owner, out _))
        {
            _popup.PopupEntity(Loc.GetString("cell-sequencer-no-connect"), ent, PopupType.MediumCaution);
            return;
        }

        if (args.CellIndex is not { } index || !TryGetDish(ent, out var dish) ||
            args.Dish != GetNetEntity(dish.Owner) ||
            args.DishRevision != dish.Comp.Revision ||
            index < 0 || index >= dish.Comp.Cells.Count)
        {
            _popup.PopupEntity(Loc.GetString("cell-sequencer-no-selected"), ent, PopupType.MediumCaution);
            return;
        }

        _database.AddCell(ent.Owner, dish.Comp.Cells[index]);
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<CellSequencerComponent> ent, ref CellSequencerUiRemoveMessage args)
    {
        if (args.Id is not { } id)
        {
            _popup.PopupEntity(Loc.GetString("cell-sequencer-no-selected"), ent, PopupType.MediumCaution);
            return;
        }

        if (!args.Remote)
        {
            if (TryGetDish(ent, out var dish) && args.Dish == GetNetEntity(dish.Owner) &&
                args.DishRevision == dish.Comp.Revision &&
                id >= 0 && id < dish.Comp.Cells.Count)
            {
                _cell.RemoveCell((dish.Owner, dish.Comp), dish.Comp.Cells[id]);
            }

            return;
        }

        if (!_database.TryGetDatabase(ent.Owner, out _))
        {
            _popup.PopupEntity(Loc.GetString("cell-sequencer-no-connect"), ent, PopupType.MediumCaution);
            return;
        }

        _database.RemoveCell(ent.Owner, id);
    }

    [SubscribeLocalEvent]
    private void OnReplace(Entity<CellSequencerComponent> ent, ref CellSequencerUiReplaceMessage args)
    {
        if (args.Id is not { } id || !TryGetDish(ent, out var dish) ||
            !_database.TryGetCell(ent.Owner, id, out var cell))
        {
            return;
        }

        var material = _materialStorage.GetMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial);
        if (material < cell.Cost ||
            !_materialStorage.TrySetMaterialAmount(ent, ent.Comp.RequiredMaterial, material - cell.Cost))
        {
            return;
        }

        _cell.ClearCells((dish.Owner, dish.Comp));
        _cell.AddCell((dish.Owner, dish.Comp), cell);
    }

    private bool TryGetDish(Entity<CellSequencerComponent> ent, out Entity<CellContainerComponent> dish)
    {
        dish = default;
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot) is not { } uid ||
            TerminatingOrDeleted(uid) || !TryComp<CellContainerComponent>(uid, out var container))
        {
            return false;
        }

        dish = (uid, container);
        return true;
    }

    private void UpdateUI(Entity<CellSequencerComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _database.TryGetDatabase(ent.Owner, out var database);
        var hasDish = TryGetDish(ent, out var dish);
        var material = _materialStorage.GetMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial);
        var state = new CellSequencerUiState(
            hasDish ? dish.Comp.Cells.ToArray() : [],
            database?.Comp.Cells.ToArray() ?? [],
            material,
            hasDish,
            hasDish ? dish.Comp.Revision : -1,
            hasDish ? GetNetEntity(dish.Owner) : null)
        {
            Connected = database != null
        };
        _userInterface.SetUiState(ent.Owner, CellSequencerUiKey.Key, state);
    }
}
