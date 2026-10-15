using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology;

/// <summary>
/// One laboratory sample with server-owned genetic material.
/// </summary>
[DataDefinition]
public sealed partial class BiologicalSample
{
    [DataField]
    public int Id { get; set; }

    [DataField, ViewVariables]
    public Color Color { get; private set; }

    [DataField, ViewVariables]
    public string Name { get; private set; } = string.Empty;

    [DataField, ViewVariables]
    public float Stability { get; private set; }

    [DataField]
    private GenomeSnapshot? _genome;

    [ViewVariables]
    public int GeneCount => _genome?.GeneCount ?? 0;

    [ViewVariables]
    public bool HasGenome => _genome is not null;

    public bool HasValidStability => float.IsFinite(Stability) && Stability is >= 0f and <= 1f;

    public BiologicalSample() { }

    public BiologicalSample(Color color, string name, float stability, GenomeSnapshot? genome)
    {
        Color = color;
        Name = name;
        Stability = stability;
        _genome = genome;
    }

    public GenomeSnapshot? GetGenome() => _genome;

    public bool TryRemoveGene(ProtoId<GenePrototype> id)
    {
        if (_genome is null)
            return false;

        var genes = new HashSet<ProtoId<GenePrototype>>(_genome.Genes);
        if (!genes.Remove(id))
            return false;

        _genome = new GenomeSnapshot(_genome.GenomeId, genes);
        return true;
    }

    /// <summary>
    /// Reduces stability without changing the stored genome or notifying observers.
    /// </summary>
    public bool TryReduceStability(float amount)
    {
        if (!HasValidStability || !float.IsFinite(amount) || amount <= 0f)
            return false;

        var reduced = Math.Clamp(Stability - amount, 0f, 1f);
        if (reduced.Equals(Stability))
            return false;

        Stability = reduced;
        return true;
    }
}
