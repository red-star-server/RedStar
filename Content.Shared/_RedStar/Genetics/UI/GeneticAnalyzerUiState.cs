using System.Linq;
using Content.Shared._RedStar.Genetics.Data;
using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared._RedStar.Genetics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Genetics.UI;

[Serializable, NetSerializable]
public enum GeneticAnalyzerUiKey : byte
{
    Key
}

/// <summary>
/// Read-only genetic report. A lost connection retains the last successful reading.
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneticAnalyzerUiState(
    NetEntity? target,
    bool inRange,
    int load,
    int maximumLoad,
    List<GeneticAnalyzerGene> genes) : BoundUserInterfaceState
{
    public readonly NetEntity? Target = target;
    public readonly bool InRange = inRange;
    public readonly int Load = load;
    public readonly int MaximumLoad = maximumLoad;
    public readonly List<GeneticAnalyzerGene> Genes = genes;
    public float LoadPercentage => GenomeSystem.CalculateLoadPercentage(Load, MaximumLoad);

    public override bool Equals(object? obj) =>
        obj is GeneticAnalyzerUiState other && Target == other.Target && InRange == other.InRange &&
        Load == other.Load && MaximumLoad == other.MaximumLoad && Genes.SequenceEqual(other.Genes);

    public override int GetHashCode() => HashCode.Combine(Target, InRange, Load, MaximumLoad, Genes.Count);
}

/// <summary>
/// A gene in the combined report. Null status means no acquired expression state is recorded.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct GeneticAnalyzerGene(ProtoId<GenePrototype> Id, GeneStatus? Status);
