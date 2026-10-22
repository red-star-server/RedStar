using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Content.Shared._RedStar.Genetics.Data;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Data;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.UI;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class SampleStabilizerSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SampleSystem _samples = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<SampleStabilizerComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            CancelStabilization(ent);
    }

    [SubscribeLocalEvent]
    private void OnDishRemoved(Entity<SampleStabilizerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.DishSlot)
            return;

        ResetStabilization(ent);
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnDishInserted(Entity<SampleStabilizerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.DishSlot)
            UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnDestruction(Entity<SampleStabilizerComponent> ent, ref DestructionEventArgs args) => CancelStabilization(ent);

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SampleStabilizerComponent> ent, ref ComponentShutdown args) => CancelStabilization(ent);

    [SubscribeLocalEvent]
    private void OnAfterOpen(Entity<SampleStabilizerComponent> ent, ref AfterActivatableUIOpenEvent args) => UpdateUI(ent);

    [SubscribeLocalEvent]
    private void OnSampleChanged(Entity<SampleStabilizerComponent> ent, ref ContainedSamplesChangedEvent args)
    {
        if (!TryGetDish(ent, out var dish) || dish.Owner != args.SampleContainer)
            return;
        ResetStabilization(ent);
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnStabilizationMessage(Entity<SampleStabilizerComponent> ent, ref SampleStabilizerUiStabilizationMessage args)
    {
        if (ent.Comp.StabilizationInProgress || !this.IsPowered(ent.Owner, EntityManager) ||
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
            _popup.PopupEntity(Loc.GetString("sample-stabilizer-incompatible"), ent, args.Actor, PopupType.MediumCaution);
            return;
        }

        ent.Comp.StabilizationSampleA = first.Id;
        ent.Comp.StabilizationSampleB = second.Id;
        ent.Comp.StabilizationDish = dish.Owner;
        ent.Comp.StabilizationEndTime = _timing.CurTime + ent.Comp.StabilizationDelay;
        Dirty(ent);
        UpdateUI(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<SampleStabilizerComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var component, out var metadata))
        {
            if (metadata.EntityPaused)
                continue;

            if (component.StabilizationEndTime is not { } endTime || _timing.CurTime < endTime)
                continue;
            var machine = new Entity<SampleStabilizerComponent>(uid, component);
            if (!this.IsPowered(uid, EntityManager) || !TryGetDish(machine, out var dish) ||
                component.StabilizationDish != dish.Owner)
            {
                CancelStabilization(machine);
                continue;
            }

            FinishStabilization(machine, dish);
        }
    }

    private void FinishStabilization(Entity<SampleStabilizerComponent> ent, Entity<SampleContainerComponent> dish)
    {
        var first = dish.Comp.Samples.Find(sample => sample.Id == ent.Comp.StabilizationSampleA);
        var second = dish.Comp.Samples.Find(sample => sample.Id == ent.Comp.StabilizationSampleB);
        if (first is null || second is null || first.Id == second.Id ||
            !CanStabilize(first, second, out var genome))
        {
            CancelStabilization(ent);
            return;
        }

        var max = Math.Max(first.Stability, second.Stability);
        var min = Math.Min(first.Stability, second.Stability);
        var stability = max + (1f - max) * (ent.Comp.BaseStabilityGain + ent.Comp.DonorStabilityGain * min);
        var result = new BiologicalSample(Color.InterpolateBetween(first.Color, second.Color, 0.5f),
            first.Name, stability, genome);
        ResetStabilization(ent);
        if (_samples.ReplaceSamples(dish.AsNullable(), first.Id, second.Id, result))
            return;

        UpdateUI(ent);
    }

    private bool CanStabilize(BiologicalSample first, BiologicalSample second, [NotNullWhen(true)] out GenomeSnapshot? genome)
    {
        genome = null;
        if (Math.Max(first.Stability, second.Stability) >= 1f ||
            first.GetGenome() is not { } firstGenome || second.GetGenome() is not { } secondGenome ||
            !firstGenome.SameContents(secondGenome))
            return false;

        genome = firstGenome;
        return true;
    }

    private bool TryGetDish(Entity<SampleStabilizerComponent> ent, out Entity<SampleContainerComponent> dish)
    {
        dish = default;
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot) is not { } uid ||
            TerminatingOrDeleted(uid) || !TryComp<SampleContainerComponent>(uid, out var container))
            return false;

        dish = (uid, container);
        return true;
    }

    private void CancelStabilization(Entity<SampleStabilizerComponent> ent)
    {
        if (!ent.Comp.StabilizationInProgress)
            return;
        ResetStabilization(ent);
        UpdateUI(ent);
    }

    private void ResetStabilization(Entity<SampleStabilizerComponent> ent)
    {
        var component = ent.Comp;
        component.StabilizationEndTime = null;
        component.StabilizationSampleA = null;
        component.StabilizationSampleB = null;
        component.StabilizationDish = null;
        Dirty(ent);
    }

    private void UpdateUI(Entity<SampleStabilizerComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var hasDish = TryGetDish(ent, out var dish);
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
        _userInterface.SetUiState(ent.Owner, SampleStabilizerUiKey.Key, new SampleStabilizerUiState(
            samples.ToArray(),
            hasDish ? GetNetEntity(dish.Owner) : null,
            ent.Comp.StabilizationInProgress,
            ent.Comp.StabilizationDelay.TotalSeconds));
    }
}
