using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

namespace Content.Server._RedStar.Xenobiology.Machines;

public sealed partial class CellSequencerSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private GeneticVialSystem _vials = default!;
    [Dependency] private SampleSystem _samples = default!;

    private byte[] _roundSeed = [];
    private readonly HashSet<ProtoId<GenePrototype>> _decoded = [];
    private readonly Dictionary<ProtoId<GenePrototype>, Puzzle> _puzzles = [];
    private sealed record Puzzle(string Sequence, string[] Fragments, int[] Order);
    private const float IncorrectStabilityCost = 0.1f;

    public override void Initialize()
    {
        base.Initialize();
        ResetRound();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => ResetRound());
    }

    private void ResetRound()
    {
        _roundSeed = RandomNumberGenerator.GetBytes(32);
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
        ClearFeedback(ent.Comp);
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
            if (GetPuzzle(id).Sequence[..SequencerPuzzle.FamilyLength] != family)
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
            ent.Comp.FeedbackSample = args.SampleId;
            ent.Comp.FeedbackFamily = args.Family;
            ent.Comp.FeedbackOrder = order.ToArray();
            _samples.TryReduceStability((dish.Owner, (SampleContainerComponent?) dish.Comp), sample!, IncorrectStabilityCost);
            _popup.PopupEntity(Loc.GetString("cell-sequencer-incorrect"), ent, PopupType.MediumCaution);
            UpdateUI(ent);
            return;
        }
        ClearFeedback(ent.Comp);
        _decoded.Add(id);
        _popup.PopupEntity(Loc.GetString("cell-sequencer-discovered", ("gene", Loc.GetString($"gene-{id}-name"))),
            ent, PopupType.Medium);
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
        ClearFeedback(ent.Comp);
        if (_vials.TryExtract(dish, sample!, vial, id))
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

    private static void ClearFeedback(CellSequencerComponent component)
    {
        component.FeedbackSample = -1;
        component.FeedbackFamily = string.Empty;
        component.FeedbackOrder = [];
    }

    private void UpdateUI(Entity<CellSequencerComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;
        if (!this.IsPowered(ent.Owner, EntityManager) || !TryGetDish(ent, out var dish))
        {
            _userInterface.SetUiState(ent.Owner, CellSequencerUiKey.Key,
                new CellSequencerUiState([], null, -1, string.Empty, [], null));
            return;
        }
        Entity<GeneticVialComponent>? vial = TryGetVial(ent, out var insertedVial) ? insertedVial : null;
        var samples = dish.Comp.Samples.Select(sample => new SequencerSampleView(new SampleView(sample),
            sample.GetGenome() is { } genome
                ? genome.Genes.OrderBy(id => id.Id, StringComparer.Ordinal)
                    .Select(id => Marker(id, sample.Stability, vial)).ToArray()
                : [])).ToArray();
        _userInterface.SetUiState(ent.Owner, CellSequencerUiKey.Key,
            new CellSequencerUiState(samples, GetNetEntity(dish.Owner), ent.Comp.FeedbackSample,
                ent.Comp.FeedbackFamily, ent.Comp.FeedbackOrder, vial is { } present ? GetNetEntity(present.Owner) : null));
    }

    private SequencerMarkerView Marker(ProtoId<GenePrototype> id, float stability, Entity<GeneticVialComponent>? vial)
    {
        var puzzle = GetPuzzle(id);
        var known = _decoded.Contains(id);
        return new SequencerMarkerView(puzzle.Sequence[..SequencerPuzzle.FamilyLength], puzzle.Fragments, known,
            known ? Loc.GetString($"gene-{id}-name") : string.Empty,
            known ? Loc.GetString("cell-sequencer-present") : string.Empty, known ? puzzle.Sequence : string.Empty,
            known && stability >= _prototypes.Index(id).MinimumStability &&
            vial is { } inserted && _vials.CanAdd(inserted, id));
    }

    private Puzzle GetPuzzle(ProtoId<GenePrototype> id)
    {
        if (_puzzles.TryGetValue(id, out var cached))
            return cached;
        var count = _prototypes.Index(id).FragmentCount;
        var length = SequencerPuzzle.Overlap + SequencerPuzzle.Step * count;
        for (var nonce = 0; ; nonce++)
        {
            var sequence = Encode($"gene:{id}:{nonce}", length);
            var family = sequence[..SequencerPuzzle.FamilyLength];
            if (_puzzles.Values.Any(puzzle => puzzle.Sequence.StartsWith(family, StringComparison.Ordinal)))
                continue;
            var pieces = Enumerable.Range(0, count).Select(i => sequence.Substring(i * SequencerPuzzle.Step, SequencerPuzzle.ReadLength)).ToArray();
            if (!Unambiguous(pieces))
                continue;
            var hash = HMACSHA256.HashData(_roundSeed, Encoding.UTF8.GetBytes($"order:{sequence}"));
            var order = Enumerable.Range(0, count).OrderBy(index => hash[index]).ThenBy(index => index).ToArray();
            var puzzle = new Puzzle(sequence, order.Select(index => pieces[index]).ToArray(), order);
            _puzzles.Add(id, puzzle);
            return puzzle;
        }
    }

    private string Encode(string value, int length)
    {
        var hash = HMACSHA256.HashData(_roundSeed, Encoding.UTF8.GetBytes(value));
        var result = new char[length];
        const string bases = "ACGT";
        for (var i = 0; i < length; i++)
        {
            result[i] = bases[hash[i] & 3];
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
