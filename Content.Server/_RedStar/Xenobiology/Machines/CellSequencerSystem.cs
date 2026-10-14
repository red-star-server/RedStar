using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Content.Server._RedStar.Genetics;
using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology.Machines;

public sealed partial class CellSequencerSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IGameTiming _timing = default!;

    private byte[] _roundSeed = [];

    public override void Initialize()
    {
        base.Initialize();
        _roundSeed = RandomNumberGenerator.GetBytes(32);
    }

    [SubscribeLocalEvent]
    private void OnOpened(Entity<CellSequencerComponent> ent, ref AfterActivatableUIOpenEvent args) => UpdateUI(ent);

    [SubscribeLocalEvent]
    private void OnInserted(Entity<CellSequencerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.DishSlot)
            return;
        Reset(ent.Comp);
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CellSequencerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.DishSlot)
            return;
        Reset(ent.Comp);
        UpdateUI(ent);
    }

    [SubscribeLocalEvent]
    private void OnCellChanged(Entity<CellContainerComponent> ent, ref CellContainerChangedEvent args)
    {
        if (_containers.TryGetContainingContainer(ent.Owner, out var container) &&
            TryComp<CellSequencerComponent>(container.Owner, out var sequencer) &&
            container.ID == sequencer.DishSlot)
            UpdateUI((container.Owner, sequencer));
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CellSequencerComponent> ent, ref PowerChangedEvent args) => UpdateUI(ent);

    [SubscribeLocalEvent]
    private void OnDecode(Entity<CellSequencerComponent> ent, ref CellSequencerDecodeMessage args)
    {
        if (!this.IsPowered(ent.Owner, EntityManager) || _timing.CurTime < ent.Comp.NextAttempt ||
            !TryGetDish(ent, out var dish) || args.Dish != GetNetEntity(dish.Owner) ||
            args.DishRevision != dish.Comp.Revision ||
            args.SampleIndex < 0 || args.SampleIndex >= dish.Comp.Cells.Count ||
            args.Order is not { Length: 4 } || args.Order.Distinct().Count() != 4 ||
            args.Order.Any(index => index is < 0 or > 3))
            return;

        var cell = dish.Comp.Cells[args.SampleIndex];
        if (cell.GetGenome() is not { } genome ||
            _genomes.ValidateSnapshot(genome) != GenomeOperationResult.Success)
            return;

        var genes = OrderedGenes(genome);
        if (args.MarkerIndex < 0 || args.MarkerIndex >= genes.Length)
            return;

        SyncWork(ent.Comp, dish);
        var (id, state) = genes[args.MarkerIndex];
        var sequence = Sequence(id, state);
        var permutation = FragmentOrder(sequence);
        for (var i = 0; i < permutation.Length; i++)
        {
            if (permutation[args.Order[i]] == i)
                continue;
            ent.Comp.NextAttempt = _timing.CurTime + TimeSpan.FromSeconds(5);
            _popup.PopupEntity(Loc.GetString("cell-sequencer-incorrect"), ent, PopupType.MediumCaution);
            UpdateUI(ent);
            return;
        }

        if (ent.Comp.Decoded.Add(sequence))
        {
            _popup.PopupEntity(Loc.GetString("cell-sequencer-discovered", ("gene", Loc.GetString($"gene-{id}-name"))),
                ent, PopupType.Medium);
        }

        UpdateUI(ent);
    }

    private bool TryGetDish(Entity<CellSequencerComponent> ent, out Entity<CellContainerComponent> dish)
    {
        dish = default;
        var uid = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.DishSlot);
        if (uid is not { } dishUid || !TryComp<CellContainerComponent>(dishUid, out var container))
            return false;
        dish = (dishUid, container);
        return true;
    }

    private static void Reset(CellSequencerComponent component)
    {
        component.ActiveDish = null;
        component.ActiveRevision = -1;
        component.Decoded.Clear();
        component.NextAttempt = TimeSpan.Zero;
    }

    private static void SyncWork(CellSequencerComponent component, Entity<CellContainerComponent> dish)
    {
        if (component.ActiveDish == dish.Owner && component.ActiveRevision == dish.Comp.Revision)
            return;
        Reset(component);
        component.ActiveDish = dish.Owner;
        component.ActiveRevision = dish.Comp.Revision;
    }

    private void UpdateUI(Entity<CellSequencerComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!this.IsPowered(ent.Owner, EntityManager) || !TryGetDish(ent, out var dish))
        {
            Reset(ent.Comp);
            _userInterface.SetUiState(ent.Owner, CellSequencerUiKey.Key,
                new CellSequencerUiState([], -1, null, 0));
            return;
        }

        SyncWork(ent.Comp, dish);

        var samples = dish.Comp.Cells.Select(cell => new SequencerSampleView(new CellView(cell),
            cell.GetGenome() is { } genome &&
            _genomes.ValidateSnapshot(genome) == GenomeOperationResult.Success
                ? OrderedGenes(genome).Select(entry => Marker(entry.Key, entry.Value, ent.Comp.Decoded)).ToArray()
                : [])).ToArray();
        var retry = Math.Max(0, (ent.Comp.NextAttempt - _timing.CurTime).TotalSeconds);
        _userInterface.SetUiState(ent.Owner, CellSequencerUiKey.Key,
            new CellSequencerUiState(samples, dish.Comp.Revision, GetNetEntity(dish.Owner), retry));
    }

    private SequencerMarkerView Marker(ProtoId<GenePrototype> id, GeneState state, HashSet<string> decoded)
    {
        var sequence = Sequence(id, state);
        var known = decoded.Contains(sequence);
        var name = known ? Loc.GetString($"gene-{id}-name") : string.Empty;
        var value = !known ? string.Empty : _prototypes.Index(id).Kind switch
        {
            GeneKind.Presence => Loc.GetString("cell-sequencer-present"),
            GeneKind.Allele => state.Allele ?? string.Empty,
            GeneKind.Number => state.Number?.ToString() ?? string.Empty,
            _ => string.Empty
        };
        var pieces = Fragments(sequence);
        var order = FragmentOrder(sequence);
        return new SequencerMarkerView(sequence[..8], order.Select(index => pieces[index]).ToArray(),
            known, name, value, known ? sequence : string.Empty);
    }

    private static KeyValuePair<ProtoId<GenePrototype>, GeneState>[] OrderedGenes(GenomeSnapshot genome) =>
        genome.GetGenes().OrderBy(entry => entry.Key.Id, StringComparer.Ordinal).ToArray();

    private static string ValueKey(ProtoId<GenePrototype> id, GeneState state) =>
        $"{id.Id}\u001f{state.Allele ?? string.Empty}\u001f{state.Number?.ToString() ?? string.Empty}";

    private string Sequence(ProtoId<GenePrototype> id, GeneState state)
    {
        var family = Encode($"gene:{id.Id}");
        var key = ValueKey(id, state);
        for (var nonce = 0; ; nonce++)
        {
            var sequence = family + Encode($"value:{key}:{nonce}");
            if (Unambiguous(Fragments(sequence)))
                return sequence;
        }
    }

    private string Encode(string value)
    {
        var hash = HMACSHA256.HashData(_roundSeed, Encoding.UTF8.GetBytes(value));
        var result = new char[8];
        const string bases = "ACGT";
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = bases[hash[i] & 3];
        }

        return new string(result);
    }

    private static string[] Fragments(string sequence) =>
        [sequence[..7], sequence[3..10], sequence[6..13], sequence[9..16]];

    private static bool Unambiguous(string[] pieces)
    {
        for (var i = 0; i < pieces.Length; i++)
        for (var j = 0; j < pieces.Length; j++)
        {
            if (pieces[i][3..] == pieces[j][..4] && j != i + 1)
                return false;
        }
        return true;
    }

    private int[] FragmentOrder(string sequence)
    {
        var hash = HMACSHA256.HashData(_roundSeed, Encoding.UTF8.GetBytes($"order:{sequence}"));
        return Enumerable.Range(0, 4).OrderBy(index => hash[index]).ThenBy(index => index).ToArray();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<CellSequencerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.NextAttempt == TimeSpan.Zero || component.NextAttempt > _timing.CurTime)
                continue;
            component.NextAttempt = TimeSpan.Zero;
            UpdateUI((uid, component));
        }
    }
}
