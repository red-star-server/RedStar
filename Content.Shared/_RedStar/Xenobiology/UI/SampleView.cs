using Content.Shared._RedStar.Xenobiology.Data;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

/// <summary>
/// Public sample details without the genetic material.
/// </summary>
[Serializable, NetSerializable]
public sealed record SampleView(int Id, string Name, Color Color, float Stability, int GeneCount, bool HasGenome)
{
    public SampleView(BiologicalSample sample)
        : this(sample.Id, sample.Name, sample.Color, sample.Stability, sample.GeneCount, sample.HasGenome) { }
}
