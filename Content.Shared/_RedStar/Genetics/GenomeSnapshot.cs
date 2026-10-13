using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Genetics;

/// <summary>
/// Independent genetic material; it never retains the donor entity.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class GenomeSnapshot
{
    [DataField]
    private Dictionary<ProtoId<GenePrototype>, GeneState> _genes = [];

    [DataField]
    public ProtoId<GenomePrototype> GenomeId { get; private set; }

    public GenomeSnapshot() { }

    public GenomeSnapshot(ProtoId<GenomePrototype> genomeId,
        IReadOnlyDictionary<ProtoId<GenePrototype>, GeneState> genes)
    {
        GenomeId = genomeId;
        _genes = genes.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());
    }

    public int GeneCount => _genes.Count;

    public GenomeSnapshot Clone() => new(GenomeId, _genes);

    public bool SameContents(GenomeSnapshot? other) =>
        other is not null && GenomeId == other.GenomeId && _genes.Count == other._genes.Count &&
        _genes.All(entry => other._genes.TryGetValue(entry.Key, out var state) &&
            entry.Value.Allele == state.Allele && entry.Value.Number == state.Number);

    public bool TryGetGene(ProtoId<GenePrototype> id, out GeneState? state)
    {
        if (_genes.TryGetValue(id, out var stored))
        {
            state = stored.Clone();
            return true;
        }

        state = null;
        return false;
    }

    public Dictionary<ProtoId<GenePrototype>, GeneState> GetGenes() =>
        _genes.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());
}
