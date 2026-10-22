using System.Linq;
using System.Text;
using Content.Server._RedStar.Genetics.Systems;
using Content.Shared.Dataset;
using Content.Shared._RedStar.Genetics.Data;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Tools;
using Content.Shared._RedStar.Xenobiology.Data;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class SampleFactorySystem : EntitySystem
{
    private static readonly ProtoId<LocalizedDatasetPrototype> SamplePrefixes = "SamplePrefixes";
    private static readonly ProtoId<LocalizedDatasetPrototype> SampleSuffixes = "SampleSuffixes";

    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private ServerGenomeSystem _genomes = default!;
    [Dependency] private SampleSystem _samples = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SampleContainerComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.InitialSamplesInitialized)
            return;

        if (ent.Comp.InitialSamples.Count > ent.Comp.MaxSamples - ent.Comp.Samples.Count)
            throw new InvalidOperationException($"Initial samples exceed the capacity of {ToPrettyString(ent)}.");

        var initial = new List<(GenomeSnapshot Genome, float Stability)>();
        foreach (var entry in ent.Comp.InitialSamples)
        {
            if (!_genomes.TryGenerate(entry.Genome, out var genome))
                throw new InvalidOperationException($"Unknown initial sample genome {entry.Genome} on {ToPrettyString(ent)}.");

            initial.Add((genome, entry.Stability));
        }

        ent.Comp.InitialSamplesInitialized = true;
        foreach (var (genome, stability) in initial)
        {
            if (!_samples.AddSample(ent.AsNullable(), CreateSample(genome, stability)))
                throw new InvalidOperationException($"Could not add an initial sample to {ToPrettyString(ent)}.");
        }
    }

    /// <summary>
    /// Creates an unassigned sample with appearance derived from its genes.
    /// Stability depends on gene count, random variation, and the donor's cellular damage.
    /// </summary>
    public BiologicalSample GenerateBiopsySample(GenomeSnapshot genome, float cellularDamage, SampleCollectorComponent settings)
    {
        var geneCount = genome.GeneCount;
        var stability = MathF.Max(settings.MinimumStability, 1f - geneCount * settings.GeneStabilityCost + _random.NextFloat(-settings.StabilityVariation, settings.StabilityVariation));
        stability *= 1f - settings.CellularDamagePenalty * Math.Clamp(cellularDamage, 0f, settings.MaximumCellularDamage) / settings.CellularDamageScale;
        return CreateSample(genome, stability);
    }

    private BiologicalSample CreateSample(GenomeSnapshot genome, float stability)
    {
        var hash = GetAppearanceHash(genome);
        var prefixes = _prototypes.Index(SamplePrefixes).Values;
        var suffixes = _prototypes.Index(SampleSuffixes).Values;
        var prefix = Loc.GetString(prefixes[(int) (hash % (ulong) prefixes.Count)]);
        var suffix = Loc.GetString(suffixes[(int) (hash / (ulong) prefixes.Count % (ulong) suffixes.Count)]);
        var color = new Color(
            0.2f + (hash & 0xff) / 255f * 0.65f,
            0.2f + (hash >> 8 & 0xff) / 255f * 0.65f,
            0.2f + (hash >> 16 & 0xff) / 255f * 0.65f);
        return new BiologicalSample(color, prefix + suffix, stability, genome);
    }

    private static ulong GetAppearanceHash(GenomeSnapshot genome)
    {
        var canonical = string.Join(";", genome.Genes.Select(id => id.Id).OrderBy(id => id, StringComparer.Ordinal));

        const ulong offset = 14695981039346656037;
        const ulong prime = 1099511628211;
        var hash = offset;
        foreach (var value in Encoding.UTF8.GetBytes(canonical))
        {
            hash = unchecked((hash ^ value) * prime);
        }

        return hash;
    }
}
