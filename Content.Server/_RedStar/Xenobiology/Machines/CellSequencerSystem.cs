using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Server._RedStar.Genetics;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Xenobiology.Machines;

public sealed partial class CellSequencerSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private GeneticVialSystem _vials = default!;
    [Dependency] private SampleSystem _samples = default!;
    [Dependency] private IRobustRandom _random = default!;

    private readonly HashSet<ProtoId<GenePrototype>> _decoded = [];
    private readonly Dictionary<ProtoId<GenePrototype>, Puzzle> _puzzles = [];
    private sealed record Puzzle(string FamilyCode, string[] Fragments, int[] Order);
    private const int MaximumPuzzleAttempts = 4096;

    public override void Initialize()
    {
        base.Initialize();
        ResetRound();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => ResetRound());
    }

    private void ResetRound()
    {
        _decoded.Clear();
        _puzzles.Clear();
    }

    [SubscribeLocalEvent]
    private void OnOpened(Entity<CellSequencerComponent> ent, ref AfterActivatableUIOpenEvent args) => UpdateUI(ent);

    [SubscribeLocalEvent]
    private void OnInserted(Entity<CellSequencerComponent> ent, ref EntInsertedIntoContainerMessage args) => UpdateUI(ent);

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CellSequencerComponent> ent, ref EntRemovedFromContainerMessage args)
    {

        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnSampleChanged(Entity<CellSequencerComponent> ent, ref ContainedSamplesChangedEvent args)
    {
        if (TryGetDish(ent, out var dish) && dish.Owner == args.SampleContainer)
            UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CellSequencerComponent> ent, ref PowerChangedEvent args) => UpdateUI(ent);

    private bool TryGetMarker(Entity<CellSequencerComponent> ent, int sampleId, string family,
        out Entity<SampleContainerComponent> dish, out BiologicalSample? sample, out ProtoId<GenePrototype> gene)
    {
        sample = null;
        gene = default;
        if (!this.IsPowered(ent.Owner, EntityManager) || !TryGetDish(ent, out dish))
        {
            dish = default;
            return false;
        }
        sample = dish.Comp.Samples.Find(s => s.Id == sampleId);
        if (sample?.GetGenome() is not { } genome)
            return false;
        foreach (var id in genome.Genes)
        {
            if (GetPuzzle(id).FamilyCode != family)
                continue;
            gene = id;
            return true;
        }
        return false;
    }

    [SubscribeLocalEvent]
    private void OnDecode(Entity<CellSequencerComponent> ent, ref CellSequencerDecodeMessage args)
    {
        if (!TryGetMarker(ent, args.SampleId, args.Family, out var dish, out var sample, out var id) ||
            _decoded.Contains(id))
            return;
        var puzzle = GetPuzzle(id);
        var count = puzzle.Order.Length;
        var order = args.Order;
        if (order.Length != count || order.Distinct().Count() != count ||
            order.Any(index => index < 0 || index >= count))
            return;
        if (Enumerable.Range(0, count).Any(i => puzzle.Order[order[i]] != i))
        {
            var matches = Enumerable.Range(1, count - 1).Select(i =>
                puzzle.Fragments[order[i - 1]][SequencerPuzzle.Step..] ==
                puzzle.Fragments[order[i]][..SequencerPuzzle.Overlap]).ToArray();
            _userInterface.ServerSendUiMessage(ent.Owner, CellSequencerUiKey.Key,
                new CellSequencerFeedbackMessage(args.SampleId, args.Family, order, matches), args.Actor);
            if (!_samples.TryReduceStability((dish.Owner, (SampleContainerComponent?) dish.Comp), args.SampleId, ent.Comp.IncorrectStabilityCost))
                UpdateUI(ent);
            _popup.PopupEntity(Loc.GetString("cell-sequencer-incorrect"), ent, args.Actor, PopupType.MediumCaution);
            return;
        }

        _decoded.Add(id);
        _popup.PopupEntity(Loc.GetString("cell-sequencer-discovered", ("gene", Loc.GetString($"gene-{id}-name"))),
            ent, args.Actor, PopupType.Medium);
        var query = EntityQueryEnumerator<CellSequencerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            UpdateUI((uid, component));
        }
    }

    [SubscribeLocalEvent]
    private void OnExtract(Entity<CellSequencerComponent> ent, ref CellSequencerExtractMessage args)
    {
        if (!TryGetMarker(ent, args.SampleId, args.Family, out var dish, out var sample, out var id) ||
            !_decoded.Contains(id) || !TryGetVial(ent, out var vial) || args.Vial != GetNetEntity(vial.Owner))
            return;

        if (!_vials.TryExtract(dish, args.SampleId, vial, id))
            UpdateUI(ent);
    }

    private bool TryGetDish(Entity<CellSequencerComponent> ent, out Entity<SampleContainerComponent> dish)
    {
        dish = default;
        var uid = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot);
        if (uid is not { } dishUid || !TryComp<SampleContainerComponent>(dishUid, out var container))
            return false;
        dish = (dishUid, container);
        return true;
    }

    private bool TryGetVial(Entity<CellSequencerComponent> ent, out Entity<GeneticVialComponent> vial)
    {
        vial = default;
        var uid = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.VialSlot);
        if (uid is not { } vialUid || !TryComp<GeneticVialComponent>(vialUid, out var component))
            return false;
        vial = (vialUid, component);
        return true;
    }

    private void UpdateUI(Entity<CellSequencerComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;
        if (!this.IsPowered(ent.Owner, EntityManager) || !TryGetDish(ent, out var dish))
        {
            _userInterface.SetUiState(ent.Owner, CellSequencerUiKey.Key,
                new CellSequencerUiState([], null, null));
            return;
        }
        Entity<GeneticVialComponent>? vial = TryGetVial(ent, out var insertedVial) ? insertedVial : null;
        var samples = dish.Comp.Samples.Select(sample => new SequencerSampleView(new SampleView(sample),
            sample.GetGenome() is { } genome
                ? genome.Genes.OrderBy(id => id.Id, StringComparer.Ordinal)
                    .Select(id => Marker(id, sample.Stability, vial)).ToArray()
                : [])).ToArray();
        _userInterface.SetUiState(ent.Owner, CellSequencerUiKey.Key,
            new CellSequencerUiState(samples, GetNetEntity(dish.Owner),
                vial is { } present ? GetNetEntity(present.Owner) : null));
    }

    private SequencerMarkerView Marker(ProtoId<GenePrototype> id, float stability, Entity<GeneticVialComponent>? vial)
    {
        var puzzle = GetPuzzle(id);
        var known = _decoded.Contains(id);
        return new SequencerMarkerView(puzzle.FamilyCode, puzzle.Fragments, known,
            known ? Loc.GetString($"gene-{id}-name") : string.Empty,
            known && stability >= _prototypes.Index(id).MinimumStability &&
            vial is { } inserted && _vials.CanAdd(inserted, id));
    }

    private Puzzle GetPuzzle(ProtoId<GenePrototype> id)
    {
        if (_puzzles.TryGetValue(id, out var cached))
            return cached;
        var count = _prototypes.Index(id).FragmentCount;
        var length = SequencerPuzzle.Overlap + SequencerPuzzle.Step * count;
        for (var attempt = 0; attempt < MaximumPuzzleAttempts; attempt++)
        {
            var sequence = GenerateSequence(length);
            var family = sequence[..SequencerPuzzle.FamilyLength];
            if (_puzzles.Values.Any(puzzle => puzzle.FamilyCode == family))
                continue;
            var pieces = Enumerable.Range(0, count).Select(i => sequence.Substring(i * SequencerPuzzle.Step, SequencerPuzzle.ReadLength)).ToArray();
            if (!Unambiguous(pieces))
                continue;
            var order = Enumerable.Range(0, count).ToArray();
            _random.Shuffle(order);
            var puzzle = new Puzzle(family, order.Select(index => pieces[index]).ToArray(), order);
            _puzzles.Add(id, puzzle);
            return puzzle;
        }

        throw new InvalidOperationException($"Could not generate an unambiguous puzzle for gene {id} after {MaximumPuzzleAttempts} attempts.");
    }

    private string GenerateSequence(int length)
    {
        var result = new char[length];
        const string bases = "ACGT";
        for (var i = 0; i < length; i++)
        {
            result[i] = bases[_random.Next(bases.Length)];
        }

        return new string(result);
    }

    private static bool Unambiguous(string[] pieces)
    {
        for (var i = 0; i < pieces.Length; i++)
        for (var j = 0; j < pieces.Length; j++)
        {
            if (pieces[i][SequencerPuzzle.Step..] == pieces[j][..SequencerPuzzle.Overlap] && j != i + 1)
                return false;
        }
        return true;
    }
}
