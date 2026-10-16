using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

/// <summary>
/// Laboratory information visible to clients without its server-owned genome.
/// </summary>
[Serializable, NetSerializable]
public sealed record SampleView(int Id, string Name, Color Color, float Stability, int GeneCount,
    bool HasGenome, int? CompatibilityGroup)
{
    public SampleView(BiologicalSample sample, int? compatibilityGroup = null)
        : this(sample.Id, sample.Name, sample.Color, sample.Stability, sample.GeneCount, sample.HasGenome, compatibilityGroup) { }
}
