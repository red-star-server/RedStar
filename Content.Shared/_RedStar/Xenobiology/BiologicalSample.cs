using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology;

/// <summary>
/// One laboratory sample with server-owned genetic material.
/// </summary>
/// <seealso cref="SampleContainerComponent"/>
/// <seealso cref="SharedSampleSystem"/>
[DataDefinition]
public sealed partial class BiologicalSample
{
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

    public BiologicalSample() { }

    public BiologicalSample(Color color, string name, float stability, GenomeSnapshot? genome)
    {
        Color = color;
        Name = name;
        Stability = stability;
        _genome = genome?.Clone();
    }

    public GenomeSnapshot? GetGenome() => _genome?.Clone();

    internal bool TryRemoveGene(ProtoId<GenePrototype> id)
    {
        if (_genome is null)
            return false;

        var genes = _genome.GetGenes();
        if (!genes.Remove(id))
            return false;

        _genome = new GenomeSnapshot(_genome.GenomeId, genes);
        return true;
    }

    /// <summary>
    /// Reduces stability without changing the stored genome or notifying observers.
    /// </summary>
    internal bool TryReduceStability(float amount)
    {
        if (!float.IsFinite(amount) || amount <= 0f)
            return false;

        var reduced = Math.Clamp(Stability - amount, 0f, 1f);
        if (reduced.Equals(Stability))
            return false;

        Stability = reduced;
        return true;
    }
}
