using System.Collections.Frozen;
using Content.Shared._RedStar.Genetics.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Data;

/// <summary>
/// Immutable genetic material, independent of its donor and expression status.
/// </summary>
[DataDefinition]
public sealed partial class GenomeSnapshot
{
    [DataField]
    private HashSet<ProtoId<GenePrototype>> _genes = [];

    private FrozenSet<ProtoId<GenePrototype>>? _view;
    /// <summary>
    /// Read-only gene set, independent of the source collection supplied to the constructor.
    /// </summary>
    public IReadOnlySet<ProtoId<GenePrototype>> Genes => _view ??= _genes.ToFrozenSet();

    [DataField]
    public ProtoId<GenomePrototype> GenomeId { get; private set; }

    public GenomeSnapshot() { }

    public GenomeSnapshot(ProtoId<GenomePrototype> genomeId, IEnumerable<ProtoId<GenePrototype>> genes)
    {
        GenomeId = genomeId;
        _genes = new HashSet<ProtoId<GenePrototype>>(genes);
    }

    public int GeneCount => _genes.Count;
    /// <summary>
    /// Compares genome prototype and gene membership; gene order and expression status are irrelevant.
    /// </summary>
    public bool SameContents(GenomeSnapshot? other) =>
        other is not null && GenomeId == other.GenomeId && Genes.SetEquals(other.Genes);
}
