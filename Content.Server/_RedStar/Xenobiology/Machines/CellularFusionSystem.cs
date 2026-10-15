using System.Linq;
using Content.Server._RedStar.Genetics;
using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
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
    [Dependency] private SharedSampleSystem _samples = default!;
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
        if (args.Container.ID != ent.Comp.DishSlot)
            return;

        CancelSplice(ent);
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnDishInserted(Entity<CellularFusionComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.DishSlot)
            UpdateUI(ent);
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
            args.DishRevision != dish.Comp.Revision || args.SampleAIndex == args.SampleBIndex ||
            args.SampleAIndex < 0 || args.SampleBIndex < 0 ||
            args.SampleAIndex >= dish.Comp.Samples.Count || args.SampleBIndex >= dish.Comp.Samples.Count)
            return;

        var first = dish.Comp.Samples[args.SampleAIndex];
        var second = dish.Comp.Samples[args.SampleBIndex];
        if (ReferenceEquals(first, second) || !CanStabilize(first, second, out _))
        {
            _popup.PopupEntity(Loc.GetString("cellular-fusion-incompatible"), ent, PopupType.MediumCaution);
            return;
        }

        ent.Comp.SpliceSampleA = first;
        ent.Comp.SpliceSampleB = second;
        ent.Comp.SpliceDish = dish.Owner;
        ent.Comp.SpliceRevision = dish.Comp.Revision;
        ent.Comp.SpliceEndTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.SpliceDelay);
        UpdateUI(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<CellularFusionComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var component, out var metadata))
        {
            if (metadata.EntityPaused)
                continue;

            var machine = new Entity<CellularFusionComponent>(uid, component);
            var hasDish = TryGetDish(machine, out var dish);
            var dishUid = hasDish ? dish.Owner : (EntityUid?) null;
            var revision = hasDish ? dish.Comp.Revision : -1;
            if (component.ObservedDish != dishUid || component.ObservedRevision != revision)
            {
                component.ObservedDish = dishUid;
                component.ObservedRevision = revision;
                UpdateUI(machine);
            }

            if (component.SpliceEndTime is not { } endTime)
                continue;

            if (!this.IsPowered(uid, EntityManager) || !hasDish ||
                component.SpliceDish != dish.Owner || component.SpliceRevision != revision)
            {
                CancelSplice(machine);
                continue;
            }

            if (_timing.CurTime >= endTime)
                FinishSplice(machine, dish);
        }
    }

    private void FinishSplice(Entity<CellularFusionComponent> ent, Entity<SampleContainerComponent> dish)
    {
        var first = ent.Comp.SpliceSampleA;
        var second = ent.Comp.SpliceSampleB;
        if (first is null || second is null || ReferenceEquals(first, second) ||
            !dish.Comp.Samples.Contains(first) || !dish.Comp.Samples.Contains(second) ||
            !CanStabilize(first, second, out var genome))
        {
            CancelSplice(ent);
            return;
        }

        var result = new BiologicalSample(SharedSampleSystem.GetStabilizedColor(first, second),
            first.Name,
            SharedSampleSystem.GetStabilizedStability(first, second), genome);
        ResetSplice(ent.Comp);
        if (!_samples.ReplaceSamples((dish.Owner, (SampleContainerComponent?) dish.Comp), first, second, result))
        {
            UpdateUI(ent);
            return;
        }
        ent.Comp.ObservedRevision = dish.Comp.Revision;
        _popup.PopupEntity(Loc.GetString("cellular-fusion-splice-success"), ent, PopupType.Medium);
        UpdateUI(ent);
    }

    private bool CanStabilize(BiologicalSample first, BiologicalSample second, out GenomeSnapshot? genome)
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

    private bool TryGetDish(Entity<CellularFusionComponent> ent, out Entity<SampleContainerComponent> dish)
    {
        dish = default;
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot) is not { } uid ||
            TerminatingOrDeleted(uid) || !TryComp<SampleContainerComponent>(uid, out var container))
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
        component.SpliceSampleA = null;
        component.SpliceSampleB = null;
        component.SpliceDish = null;
        component.SpliceRevision = 0;
    }

    private void UpdateUI(Entity<CellularFusionComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var hasDish = TryGetDish(ent, out var dish);
        var remaining = ent.Comp.SpliceEndTime is { } endTime
            ? Math.Max(0, (endTime - _timing.CurTime).TotalSeconds)
            : 0;
        _userInterface.SetUiState(ent.Owner, CellularFusionUiKey.Key, new CellularFusionUiState(
            hasDish ? dish.Comp.Samples.Select(cell => new SampleView(cell)).ToArray() : [],
            hasDish ? dish.Comp.Revision : -1,
            hasDish ? GetNetEntity(dish.Owner) : null,
            ent.Comp.SpliceInProgress,
            ent.Comp.SpliceDelay,
            remaining));
    }
}
