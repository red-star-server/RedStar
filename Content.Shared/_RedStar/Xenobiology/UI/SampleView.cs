using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

/// <summary>
/// Public sample details without the genetic material.
/// CompatibilityGroup identifies equal genomes within one UI state; it is not a persistent genome ID.
/// </summary>
[Serializable, NetSerializable]
public sealed record SampleView(int Id, string Name, Color Color, float Stability, int GeneCount,
    bool HasGenome, int? CompatibilityGroup)
{
    public SampleView(BiologicalSample sample, int? compatibilityGroup = null)
        : this(sample.Id, sample.Name, sample.Color, sample.Stability, sample.GeneCount, sample.HasGenome, compatibilityGroup) { }
}
