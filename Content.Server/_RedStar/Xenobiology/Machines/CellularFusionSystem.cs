using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Events;
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
    [Dependency] private SampleSystem _samples = default!;
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

        ResetSplice(ent.Comp);
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
    private void OnSampleChanged(Entity<CellularFusionComponent> ent, ref ContainedSamplesChangedEvent args)
    {
        if (!TryGetDish(ent, out var dish) || dish.Owner != args.SampleContainer)
            return;
        ResetSplice(ent.Comp);
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnSpliceMessage(Entity<CellularFusionComponent> ent, ref CellularFusionUiSpliceMessage args)
    {
        if (ent.Comp.SpliceInProgress || !this.IsPowered(ent.Owner, EntityManager) ||
            !TryGetDish(ent, out var dish) || args.SampleAId == args.SampleBId)
            return;
        var firstId = args.SampleAId;
        var secondId = args.SampleBId;
        var first = dish.Comp.Samples.Find(sample => sample.Id == firstId);
        var second = dish.Comp.Samples.Find(sample => sample.Id == secondId);
        if (first is null || second is null)
            return;
        if (!CanStabilize(first, second, out _))
        {
            _popup.PopupEntity(Loc.GetString("cellular-fusion-incompatible"), ent, args.Actor, PopupType.MediumCaution);
            return;
        }

        ent.Comp.SpliceSampleA = first.Id;
        ent.Comp.SpliceSampleB = second.Id;
        ent.Comp.SpliceDish = dish.Owner;
        ent.Comp.SpliceEndTime = _timing.CurTime + ent.Comp.SpliceDelay;
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

            if (component.SpliceEndTime is not { } endTime || _timing.CurTime < endTime)
                continue;
            var machine = new Entity<CellularFusionComponent>(uid, component);
            if (!this.IsPowered(uid, EntityManager) || !TryGetDish(machine, out var dish) ||
                component.SpliceDish != dish.Owner)
            {
                CancelSplice(machine);
                continue;
            }

            FinishSplice(machine, dish);
        }
    }

    private void FinishSplice(Entity<CellularFusionComponent> ent, Entity<SampleContainerComponent> dish)
    {
        var first = dish.Comp.Samples.Find(sample => sample.Id == ent.Comp.SpliceSampleA);
        var second = dish.Comp.Samples.Find(sample => sample.Id == ent.Comp.SpliceSampleB);
        if (first is null || second is null || first.Id == second.Id ||
            !CanStabilize(first, second, out var genome))
        {
            CancelSplice(ent);
            return;
        }

        var max = Math.Max(first.Stability, second.Stability);
        var min = Math.Min(first.Stability, second.Stability);
        var stability = max + (1f - max) * (ent.Comp.BaseStabilityGain + ent.Comp.DonorStabilityGain * min);
        var result = new BiologicalSample(Color.InterpolateBetween(first.Color, second.Color, 0.5f),
            first.Name, stability, genome);
        ResetSplice(ent.Comp);
        if (!_samples.ReplaceSamples((dish.Owner, (SampleContainerComponent?) dish.Comp), first.Id, second.Id, result))
        {
            UpdateUI(ent);
            return;
        }
    }

    private bool CanStabilize(BiologicalSample first, BiologicalSample second, out GenomeSnapshot? genome)
    {
        genome = null;
        if (Math.Max(first.Stability, second.Stability) >= 1f ||
            first.GetGenome() is not { } firstGenome || second.GetGenome() is not { } secondGenome ||
            !firstGenome.SameContents(secondGenome))
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
    }

    private void UpdateUI(Entity<CellularFusionComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var hasDish = TryGetDish(ent, out var dish);
        var remaining = ent.Comp.SpliceEndTime is { } endTime
            ? Math.Max(0, (endTime - _timing.CurTime).TotalSeconds)
            : 0;
        var samples = new List<SampleView>();
        var genomes = new List<GenomeSnapshot>();
        if (hasDish)
        {
            foreach (var sample in dish.Comp.Samples)
            {
                int? group = null;
                if (sample.GetGenome() is { } genome)
                {
                    var index = genomes.FindIndex(existing => existing.SameContents(genome));
                    if (index < 0)
                    {
                        index = genomes.Count;
                        genomes.Add(genome);
                    }
                    group = index;
                }
                samples.Add(new SampleView(sample, group));
            }
        }
        _userInterface.SetUiState(ent.Owner, CellularFusionUiKey.Key, new CellularFusionUiState(
            samples.ToArray(),
            hasDish ? GetNetEntity(dish.Owner) : null,
            ent.Comp.SpliceInProgress,
            ent.Comp.SpliceDelay.TotalSeconds,
            remaining));
    }
}
