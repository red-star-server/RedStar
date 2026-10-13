using Content.Shared._RedStar.Genetics;
using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Independent genetic material; it never retains the donor entity.
/// </summary>
public sealed partial class GenomeSnapshot
{
    private readonly Dictionary<ProtoId<GenePrototype>, GeneState> _genes;

    public ProtoId<GenomePrototype> GenomeId { get; }

    public GenomeSnapshot(ProtoId<GenomePrototype> genomeId,
        IReadOnlyDictionary<ProtoId<GenePrototype>, GeneState> genes)
    {
        GenomeId = genomeId;
        _genes = genes.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());
    }

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
