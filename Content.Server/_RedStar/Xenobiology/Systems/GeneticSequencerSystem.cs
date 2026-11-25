using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Server._RedStar.Genetics.Systems;
using Content.Server._RedStar.Xenobiology.Helpers;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Data;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.UI;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Puzzle = Content.Server._RedStar.Xenobiology.Helpers.SequencerPuzzleGenerator.Puzzle;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class GeneticSequencerSystem : EntitySystem
{
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private GeneticVialSystem _vials = default!;
    [Dependency] private SampleSystem _samples = default!;
    [Dependency] private GeneResearchSystem _geneResearch = default!;
    [Dependency] private IRobustRandom _random = default!;

    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<BiologicalSample, Dictionary<ProtoId<GenePrototype>, Puzzle>> _puzzles = new();

    [SubscribeLocalEvent]
    private void OnOpened(Entity<GeneticSequencerComponent> ent, ref AfterActivatableUIOpenEvent args) => UpdateUI(ent);

    [SubscribeLocalEvent]
    private void OnInserted(Entity<GeneticSequencerComponent> ent, ref EntInsertedIntoContainerMessage args) => UpdateUI(ent);

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<GeneticSequencerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnSampleChanged(Entity<GeneticSequencerComponent> ent, ref ContainedSamplesChangedEvent args)
    {
        if (TryGetDish(ent, out var dish) && dish.Owner == args.SampleContainer)
            UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<GeneticSequencerComponent> ent, ref PowerChangedEvent args) => UpdateUI(ent);

    private bool TryGetMarker(Entity<GeneticSequencerComponent> ent, int sampleId, string family,
        out Entity<SampleContainerComponent> dish, [NotNullWhen(true)] out BiologicalSample? sample,
        out ProtoId<GenePrototype> gene)
    {
        gene = default;
        sample = null;
        if (!_power.IsPowered(ent.Owner) || !TryGetDish(ent, out dish))
        {
            dish = default;
            return false;
        }
        sample = dish.Comp.Samples.Find(s => s.Id == sampleId);
        if (sample?.GetGenome() is not { } genome)
            return false;
        foreach (var id in genome.Genes)
        {
            if (GetPuzzle(sample, id).FamilyCode != family)
                continue;
            gene = id;
            return true;
        }
        return false;
    }

    [SubscribeLocalEvent]
    private void OnDecode(Entity<GeneticSequencerComponent> ent, ref GeneticSequencerDecodeMessage args)
    {
        if (!TryGetMarker(ent, args.SampleId, args.Family, out var dish, out var sample, out var id) ||
            sample.IsDecoded(id))
            return;
        var puzzle = GetPuzzle(sample, id);
        var count = puzzle.Order.Length;
        if (args.Order is not { } order || order.Length != count || order.Distinct().Count() != count ||
            order.Any(index => index < 0 || index >= count))
            return;
        if (Enumerable.Range(0, count).Any(i => puzzle.Order[order[i]] != i))
        {
            var matches = Enumerable.Range(1, count - 1).Select(i =>
                puzzle.Fragments[order[i - 1]][SequencerPuzzle.Step..] ==
                puzzle.Fragments[order[i]][..SequencerPuzzle.Overlap]).ToArray();
            _userInterface.ServerSendUiMessage(ent.Owner, GeneticSequencerUiKey.Key,
                new GeneticSequencerFeedbackMessage(args.SampleId, args.Family, order, matches), args.Actor);
            if (!_samples.TryReduceStability(dish.AsNullable(), args.SampleId, ent.Comp.IncorrectStabilityCost))
                UpdateUI(ent);
            _popup.PopupEntity(Loc.GetString("genetic-sequencer-incorrect"), ent, args.Actor, PopupType.MediumCaution);
            return;
        }

        if (!_samples.TryMarkDecoded(dish.AsNullable(), args.SampleId, id))
            return;

        _geneResearch.TryRegisterDiscovery(ent.Owner, id, out _);
        _popup.PopupEntity(Loc.GetString("genetic-sequencer-discovered", ("gene", Loc.GetString(_prototypes.Index(id).Name))),
            ent, args.Actor, PopupType.Medium);
    }

    [SubscribeLocalEvent]
    private void OnExtract(Entity<GeneticSequencerComponent> ent, ref GeneticSequencerExtractMessage args)
    {
        if (!TryGetMarker(ent, args.SampleId, args.Family, out var dish, out var sample, out var id) ||
            !sample.IsDecoded(id) || !TryGetVial(ent, out var vial) || args.Vial != GetNetEntity(vial.Owner))
            return;

        if (!_vials.TryExtract(dish, args.SampleId, vial, id))
            UpdateUI(ent);
    }

    private bool TryGetDish(Entity<GeneticSequencerComponent> ent, out Entity<SampleContainerComponent> dish)
    {
        dish = default;
        var uid = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot);
        if (uid is not { } dishUid || !TryComp<SampleContainerComponent>(dishUid, out var container))
            return false;
        dish = (dishUid, container);
        return true;
    }

    private bool TryGetVial(Entity<GeneticSequencerComponent> ent, out Entity<GeneticVialComponent> vial)
    {
        vial = default;
        var uid = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.VialSlot);
        if (uid is not { } vialUid || !TryComp<GeneticVialComponent>(vialUid, out var component))
            return false;
        vial = (vialUid, component);
        return true;
    }

    private void UpdateUI(Entity<GeneticSequencerComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;
        if (!_power.IsPowered(ent.Owner) || !TryGetDish(ent, out var dish))
        {
            _userInterface.SetUiState(ent.Owner, GeneticSequencerUiKey.Key,
                new GeneticSequencerUiState([], null, null));
            return;
        }
        Entity<GeneticVialComponent>? vial = TryGetVial(ent, out var insertedVial) ? insertedVial : null;
        var samples = dish.Comp.Samples.Select(sample => new SequencerSampleView(new SampleView(sample),
            sample.GetGenome() is { } genome
                ? genome.Genes.OrderBy(id => id.Id, StringComparer.Ordinal)
                    .Select(id => Marker(sample, id, vial)).ToArray()
                : [])).ToArray();
        _userInterface.SetUiState(ent.Owner, GeneticSequencerUiKey.Key,
            new GeneticSequencerUiState(samples, GetNetEntity(dish.Owner),
                vial is { } present ? GetNetEntity(present.Owner) : null));
    }

    private SequencerMarkerView Marker(BiologicalSample sample, ProtoId<GenePrototype> id,
        Entity<GeneticVialComponent>? vial)
    {
        var puzzle = GetPuzzle(sample, id);
        var known = sample.IsDecoded(id);
        return new SequencerMarkerView(puzzle.FamilyCode, puzzle.Fragments, known,
            known ? Loc.GetString(_prototypes.Index(id).Name) : string.Empty,
            known && vial is { } inserted && _vials.CanAdd(inserted, id));
    }

    private Puzzle GetPuzzle(BiologicalSample sample, ProtoId<GenePrototype> id)
    {
        var puzzles = _puzzles.GetOrCreateValue(sample);
        if (puzzles.TryGetValue(id, out var cached))
            return cached;

        var puzzle = SequencerPuzzleGenerator.Generate(_random, _prototypes.Index(id).FragmentCount,
            family => puzzles.Values.Any(existing => existing.FamilyCode == family));
        puzzles.Add(id, puzzle);
        return puzzle;
    }
}
