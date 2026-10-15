using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

/// <summary>
/// Laboratory information visible to clients without its server-owned genome.
/// </summary>
[Serializable, NetSerializable]
public sealed class SampleView(BiologicalSample sample)
{
    public readonly int Id = sample.Id;
    public readonly string Name = sample.Name;
    public readonly Color Color = sample.Color;
    public readonly float Stability = sample.Stability;
    public readonly int GeneCount = sample.GeneCount;
    public readonly bool HasGenome = sample.HasGenome;

    public bool SameDisplay(SampleView other) =>
        Id == other.Id && Name == other.Name && Color == other.Color && Stability.Equals(other.Stability) &&
        GeneCount == other.GeneCount && HasGenome == other.HasGenome;
}
