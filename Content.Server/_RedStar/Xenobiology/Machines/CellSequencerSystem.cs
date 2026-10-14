using System.Linq;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;

namespace Content.Server._RedStar.Xenobiology.Machines;

public sealed partial class CellSequencerSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private CellularFusionSystem _fusion = default!;

    [SubscribeLocalEvent]
    private void OnOpened(Entity<CellSequencerComponent> ent, ref AfterActivatableUIOpenEvent args) => UpdateUI(ent);

    [SubscribeLocalEvent]
    private void OnInserted(Entity<CellSequencerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.DishSlot)
            UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CellSequencerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.DishSlot)
            UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnCellChanged(Entity<CellContainerComponent> ent, ref CellContainerChangedEvent args)
    {
        _fusion.OnDishCellChanged(ent);
        if (_containers.TryGetContainingContainer(ent.Owner, out var container) &&
            TryComp<CellSequencerComponent>(container.Owner, out var sequencer) &&
            container.ID == sequencer.DishSlot)
            UpdateUI((container.Owner, sequencer));
    }

    private void UpdateUI(Entity<CellSequencerComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var dish = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot);
        var cells = dish is { } uid && TryComp<CellContainerComponent>(uid, out var container)
            ? container.Cells.Select(cell => new CellView(cell)).ToArray()
            : [];
        _userInterface.SetUiState(ent.Owner, CellSequencerUiKey.Key, new CellSequencerUiState(cells));
    }
}
