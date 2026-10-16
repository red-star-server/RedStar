using System.Linq;
using System.Text;
using Content.Server._RedStar.Genetics;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Tools;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Xenobiology;

public sealed partial class SampleFactorySystem : EntitySystem
{
    private static readonly ProtoId<LocalizedDatasetPrototype> SamplePrefixes = "CellPrefixes";
    private static readonly ProtoId<LocalizedDatasetPrototype> SampleSuffixes = "CellSuffixes";

    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private SampleSystem _samples = default!;

    [SubscribeLocalEvent]
    private void OnSampleGenerationInit(Entity<SampleGenerationComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Generated || !TryComp<SampleContainerComponent>(ent, out var container))
            return;

        ent.Comp.Generated = true;
        foreach (var entry in ent.Comp.Samples)
        {
            if (!_genomes.TryGenerate(entry.Genome, out var genome) || genome is null)
                continue;

            _samples.AddSample((ent, container), CreateSample(genome, entry.Stability));
        }
    }

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
