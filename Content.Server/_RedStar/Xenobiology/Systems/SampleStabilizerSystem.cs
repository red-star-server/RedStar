using System.Linq;
using Content.Server._RedStar.Xenobiology.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Data;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.Chemistry.Components.SolutionManager;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class SampleStabilizerSystem : EntitySystem
{
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SampleSystem _samples = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<SampleStabilizerComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            CancelStabilization(ent);
    }

    [SubscribeLocalEvent]
    private void OnSlotRemoved(Entity<SampleStabilizerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.DishSlot && args.Container.ID != ent.Comp.BeakerSlot)
            return;

        ResetStabilization(ent);
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnSlotInserted(Entity<SampleStabilizerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.DishSlot || args.Container.ID == ent.Comp.BeakerSlot)
            UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnBeakerSolutionChanged(Entity<SolutionManagerComponent> beaker, ref SolutionChangedEvent args)
    {
        if (!_containers.TryGetContainingContainer(beaker.Owner, out var container) ||
            !TryComp<SampleStabilizerComponent>(container.Owner, out var stabilizer) ||
            container.ID != stabilizer.BeakerSlot ||
            _itemSlots.GetItemOrNull(container.Owner, stabilizer.BeakerSlot) != beaker.Owner)
            return;

        UpdateUI((container.Owner, stabilizer));
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
        var sampleId = args.SampleId;
        if (ent.Comp.StabilizationInProgress || !_power.IsPowered(ent.Owner) ||
            !TryGetDish(ent, out var dish) || dish.Owner != GetEntity(args.Dish) ||
            dish.Comp.Samples.Find(s => s.Id == sampleId) is not { } sample)
            return;

        if (!TryGetBeaker(ent, out var beaker, out var solution))
        {
            _popup.PopupEntity(Loc.GetString("sample-stabilizer-no-beaker"), ent, args.Actor, PopupType.MediumCaution);
            return;
        }

        if (!TryGetCost(ent.Comp, sample, out _, out var cost))
        {
            _popup.PopupEntity(Loc.GetString("sample-stabilizer-nothing-to-stabilize"), ent, args.Actor, PopupType.MediumCaution);
            return;
        }

        if (GetReagentAmount(ent.Comp, solution) < cost)
        {
            _popup.PopupEntity(Loc.GetString("sample-stabilizer-not-enough-reagent"), ent, args.Actor, PopupType.MediumCaution);
            return;
        }

        ent.Comp.StabilizationSample = sample.Id;
        ent.Comp.StabilizationDish = dish.Owner;
        ent.Comp.StabilizationBeaker = beaker;
        ent.Comp.StabilizationEndTime = _timing.CurTime + ent.Comp.StabilizationDelay;
        EnsureComp<ActiveSampleStabilizerComponent>(ent);
        Dirty(ent);
        UpdateUI(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ActiveSampleStabilizerComponent, SampleStabilizerComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var component, out var metadata))
        {
            if (metadata.EntityPaused || component.StabilizationEndTime is not { } endTime || _timing.CurTime < endTime)
                continue;

            var machine = new Entity<SampleStabilizerComponent>(uid, component);
            if (!_power.IsPowered(uid) || !TryGetDish(machine, out var dish) ||
                component.StabilizationDish != dish.Owner || !TryGetBeaker(machine, out var beaker, out var solution) ||
                component.StabilizationBeaker != beaker)
            {
                CancelStabilization(machine);
                continue;
            }

            FinishStabilization(machine, dish, solution);
        }
    }

    private void FinishStabilization(Entity<SampleStabilizerComponent> ent, Entity<SampleContainerComponent> dish,
        Entity<SolutionComponent> solution)
    {
        var sampleId = ent.Comp.StabilizationSample;
        if (dish.Comp.Samples.Find(s => s.Id == sampleId) is not { } sample ||
            !TryGetCost(ent.Comp, sample, out var gain, out var cost) || GetReagentAmount(ent.Comp, solution) < cost)
        {
            CancelStabilization(ent);
            return;
        }

        _solutions.RemoveReagent(solution, ent.Comp.Reagent.Id, cost);
        ResetStabilization(ent);
        if (_samples.TryRestoreStability(dish.AsNullable(), sample.Id, gain))
            return;

        UpdateUI(ent);
    }

    private static bool TryGetCost(SampleStabilizerComponent component, BiologicalSample sample, out float gain,
        out FixedPoint2 cost)
    {
        gain = MathF.Min(component.MaximumGain, 1f - sample.Stability);
        cost = FixedPoint2.New(gain / component.StabilityPerUnit);
        return sample.HasGenome && gain > 0f && cost > FixedPoint2.Zero;
    }

    private static FixedPoint2 GetReagentAmount(SampleStabilizerComponent component, Entity<SolutionComponent> solution) =>
        solution.Comp.Solution.GetReagentQuantity(new ReagentId(component.Reagent.Id, null));

    private bool TryGetDish(Entity<SampleStabilizerComponent> ent, out Entity<SampleContainerComponent> dish)
    {
        dish = default;
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot) is not { } uid ||
            TerminatingOrDeleted(uid) || !TryComp<SampleContainerComponent>(uid, out var container))
            return false;

        dish = (uid, container);
        return true;
    }

    private bool TryGetBeaker(Entity<SampleStabilizerComponent> ent, out EntityUid beaker, out Entity<SolutionComponent> solution)
    {
        beaker = default;
        solution = default;
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.BeakerSlot) is not { } uid ||
            TerminatingOrDeleted(uid) || !_solutions.TryGetFitsInDispenser(uid, out var entity, out _))
            return false;

        beaker = uid;
        solution = entity.Value;
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
        component.StabilizationSample = null;
        component.StabilizationDish = null;
        component.StabilizationBeaker = null;
        RemComp<ActiveSampleStabilizerComponent>(ent);
        Dirty(ent);
    }

    private void UpdateUI(Entity<SampleStabilizerComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var hasDish = TryGetDish(ent, out var dish);
        var hasBeaker = TryGetBeaker(ent, out _, out var solution);
        _userInterface.SetUiState(ent.Owner, SampleStabilizerUiKey.Key, new SampleStabilizerUiState(
            hasDish ? dish.Comp.Samples.Select(sample => new SampleView(sample)).ToArray() : [],
            hasDish ? GetNetEntity(dish.Owner) : null,
            hasBeaker,
            ent.Comp.Reagent,
            hasBeaker ? GetReagentAmount(ent.Comp, solution) : FixedPoint2.Zero,
            ent.Comp.StabilityPerUnit,
            ent.Comp.MaximumGain,
            ent.Comp.StabilizationInProgress,
            ent.Comp.StabilizationDelay.TotalSeconds));
    }
}
