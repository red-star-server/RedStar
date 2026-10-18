using Content.Shared._RedStar.Genetics.Data;
using Content.Shared._RedStar.Genetics.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Data;

/// <summary>
/// One laboratory sample with server-owned genetic material.
/// </summary>
[DataDefinition]
public sealed partial class BiologicalSample
{
    /// <summary>
    /// Sample identity retained during transfer. Zero denotes a new sample awaiting an ID from SampleSystem.
    /// </summary>
    [DataField]
    public int Id { get; set; }

    [DataField, ViewVariables]
    public Color Color { get; private set; }

    [DataField, ViewVariables]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Sample integrity in the range zero to one. Non-finite values are treated as zero.
    /// </summary>
    [DataField, ViewVariables]
    public float Stability
    {
        get;
        private set => field = float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : 0f;
    }

    [DataField]
    private GenomeSnapshot? _genome;

    [ViewVariables]
    public int GeneCount => _genome?.GeneCount ?? 0;

    [ViewVariables]
    public bool HasGenome => _genome is not null;

    public BiologicalSample() { }

    public BiologicalSample(Color color, string name, float stability, GenomeSnapshot? genome)
    {
        Color = color;
        Name = name;
        Stability = stability;
        _genome = genome;
    }

    /// <summary>
    /// Returns the immutable genetic material, or null if the sample has no usable genome.
    /// </summary>
    public GenomeSnapshot? GetGenome() => _genome;

    /// <summary>
    /// Removes a gene by replacing the immutable genome snapshot. Does not notify container observers.
    /// </summary>
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
    /// Reduces stability and destroys the genome at zero stability, without notifying observers.
    /// </summary>
    public bool TryReduceStability(float amount)
    {
        if (amount <= 0f)
            return false;

        var previous = Stability;
        Stability -= amount;
        if (Stability <= 0f)
            _genome = null;
        return !Stability.Equals(previous);
    }
}
