using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Systems;
using Content.Shared._RedStar.Xenobiology.Systems.Machines;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology;
public sealed partial class MutagenicInjectorSystem : SharedMutagenicInjectorSystem
{
    [Dependency] private CellSystem _cells = default!;
    [Dependency] private SharedCellSystem _cell = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    [SubscribeLocalEvent]
    private void OnActivateInWorld(Entity<CellMutagenicInjectorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        TryStartInjection(ent);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CellMutagenicInjectorComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            CancelInjection(ent);
    }

    [SubscribeLocalEvent]
    private void OnContainerRemoved(Entity<CellMutagenicInjectorComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.DishSlot || args.Container.ID == CellMutagenicInjectorComponent.BodyContainerId)
            CancelInjection(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CellMutagenicInjectorComponent> ent, ref ComponentShutdown args)
    {
        CancelInjection(ent);
    }

    private void TryStartInjection(Entity<CellMutagenicInjectorComponent> ent)
    {
        if (ent.Comp.InjectionEndTime is not null || !this.IsPowered(ent.Owner, EntityManager) ||
            !ValidateInjection(ent))
            return;

        var dish = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot)!.Value;
        var dishContainer = Comp<CellContainerComponent>(dish);

        ent.Comp.InjectionSubject = ent.Comp.BodyContainer.ContainedEntity;
        ent.Comp.InjectionDish = dish;
        ent.Comp.InjectionDishRevision = dishContainer.Revision;
        ent.Comp.InjectionEndTime = _timing.CurTime + ent.Comp.InjectionDelay;
        ent.Comp.AudioStream = _audio.PlayPvs(ent.Comp.ProcessSound, ent.Owner)?.Entity;
    }

    private bool ValidateInjection(Entity<CellMutagenicInjectorComponent> ent)
    {
        var dish = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot);
        if (dish is null)
        {
            _popup.PopupEntity(Loc.GetString("mutagenic-injector-no-dish"), ent, PopupType.MediumCaution);
            return false;
        }

        if (!TryComp<CellContainerComponent>(dish.Value, out var dishContainer) || dishContainer.Empty)
        {
            _popup.PopupEntity(Loc.GetString("mutagenic-injector-no-cells"), ent, PopupType.MediumCaution);
            return false;
        }

        if (!dishContainer.Cells.Exists(cell => !cell.Traits.IsEmpty))
        {
            _popup.PopupEntity(Loc.GetString("mutagenic-injector-no-traits"), ent, PopupType.MediumCaution);
            return false;
        }

        if (ent.Comp.BodyContainer.ContainedEntity is { } recipient && IsValidRecipient(recipient))
            return true;

        _popup.PopupEntity(Loc.GetString("mutagenic-injector-no-target"), ent, PopupType.MediumCaution);
        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CellMutagenicInjectorComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var component, out var metadata))
        {
            if (metadata.EntityPaused || component.InjectionEndTime is not { } endTime)
                continue;

            var machine = new Entity<CellMutagenicInjectorComponent>(uid, component);
            if (!this.IsPowered(uid, EntityManager) ||
                component.InjectionSubject != component.BodyContainer.ContainedEntity ||
                component.InjectionDish != _itemSlots.GetItemOrNull(uid, component.DishSlot))
            {
                CancelInjection(machine);
                continue;
            }

            if (_timing.CurTime >= endTime)
                FinishInjection(machine);
        }
    }

    private void FinishInjection(Entity<CellMutagenicInjectorComponent> ent)
    {
        var subject = ent.Comp.InjectionSubject;
        var dish = ent.Comp.InjectionDish;
        if (subject is not { } mob || dish is not { } dishUid ||
            !TryComp<CellContainerComponent>(dishUid, out var dishContainer) ||
            dishContainer.Revision != ent.Comp.InjectionDishRevision || dishContainer.Empty ||
            !IsValidRecipient(mob))
        {
            CancelInjection(ent);
            return;
        }

        var samples = dishContainer.Cells.ToArray();
        _cells.GetOrCreateNativeSample(mob);
        foreach (var sample in samples)
            _cell.ApplyCellTraits(mob, sample);

        _cell.ClearCells((dishUid, dishContainer));
        EjectBody(ent);
        var traits = string.Join(", ", samples.SelectMany(sample => sample.Traits).Distinct()
            .Select(id => Loc.GetString(_prototypes.Index(id).Name)));
        _popup.PopupEntity(Loc.GetString("mutagenic-injector-success", ("traits", traits)), ent, PopupType.Medium);
    }

    protected override void CancelInjection(Entity<CellMutagenicInjectorComponent> ent)
    {
        if (ent.Comp.InjectionEndTime is null)
            return;

        ent.Comp.InjectionEndTime = null;
        ent.Comp.InjectionSubject = null;
        ent.Comp.InjectionDish = null;
        ent.Comp.InjectionDishRevision = 0;
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
    }
}
