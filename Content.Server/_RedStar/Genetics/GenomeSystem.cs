using System.Linq;
using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Owns generation, inheritance and all changes to an organism's genome.
/// </summary>
public sealed partial class GenomeSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IRobustRandom _random = default!;

    private GenomeOperationResult ValidateScheme(GenomePrototype scheme)
    {
        var seen = new HashSet<ProtoId<GenePrototype>>();
        foreach (var definition in scheme.Genes)
        {
            if (!_prototypes.TryIndex(definition.Gene, out var gene))
                return GenomeOperationResult.UnknownGene;
            if (!seen.Add(definition.Gene) || !ValidDefinition(definition, gene))
                return GenomeOperationResult.InvalidValue;
        }

        return GenomeOperationResult.Success;
    }

    private GenomeOperationResult CheckScheme(ProtoId<GenomePrototype> id)
    {
        return !_prototypes.TryIndex(id, out var scheme) ? GenomeOperationResult.UnknownScheme : ValidateScheme(scheme);
    }

    private GenomeOperationResult ValidateGenome(ProtoId<GenomePrototype> id,
        IReadOnlyDictionary<ProtoId<GenePrototype>, GeneState> genes)
    {
        var result = CheckScheme(id);
        if (result != GenomeOperationResult.Success)
            return result;

        foreach (var (geneId, state) in genes)
        {
            if (!_prototypes.TryIndex(geneId, out var gene))
                return GenomeOperationResult.UnknownGene;
            if (!ValidState(state, gene))
                return GenomeOperationResult.InvalidValue;
        }

        return GenomeOperationResult.Success;
    }

    public GenomeOperationResult ValidateSnapshot(GenomeSnapshot snapshot) =>
        ValidateGenome(snapshot.GenomeId, snapshot.GetGenes());

    public GenomeOperationResult ValidateGene(ProtoId<GenePrototype> id, GeneState state)
    {
        if (!_prototypes.TryIndex(id, out var gene))
            return GenomeOperationResult.UnknownGene;
        return ValidState(state, gene) ? GenomeOperationResult.Success : GenomeOperationResult.InvalidValue;
    }

    /// <summary>
    /// Checks every incoming gene before a procedure starts or modifies a genome.
    /// </summary>
    public GenomeOperationResult ValidateGenes(IReadOnlyDictionary<ProtoId<GenePrototype>, GeneState> genes)
    {
        foreach (var (id, state) in genes)
        {
            var result = ValidateGene(id, state);
            if (result != GenomeOperationResult.Success)
                return result;
        }

        return GenomeOperationResult.Success;
    }

    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<GenomeComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.GenomeGenerated || ent.Comp.Genes.Count > 0)
        {
            var result = ValidateGenome(ent.Comp.GenomeId, ent.Comp.Genes);
            if (result != GenomeOperationResult.Success)
            {
                Log.Error($"Invalid loaded genome for {ToPrettyString(ent)}: {result}");
                ent.Comp.Genes = [];
                ent.Comp.GenomeGenerated = false;
                return;
            }

            ent.Comp.GenomeGenerated = true;
        }

        // A copied entity must not share mutable gene states with its source.
        ent.Comp.Genes = ent.Comp.Genes.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GenomeComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.GenomeGenerated)
        {
            RaiseLocalEvent(ent.Owner, new GenomeReadyEvent());
            return;
        }

        var result = GenerateGenes(ent.Comp.GenomeId, out var genes);
        if (result != GenomeOperationResult.Success)
        {
            Log.Error($"Could not generate genome {ent.Comp.GenomeId} for {ToPrettyString(ent)}: {result}");
            return;
        }

        Store(ent, genes);
    }

    public GenomeOperationResult Generate(ProtoId<GenomePrototype> id, out GenomeSnapshot? genome)
    {
        genome = null;
        var result = GenerateGenes(id, out var genes);
        if (result == GenomeOperationResult.Success)
            genome = new GenomeSnapshot(id, genes);
        return result;
    }

    private GenomeOperationResult GenerateGenes(ProtoId<GenomePrototype> id,
        out Dictionary<ProtoId<GenePrototype>, GeneState> genes)
    {
        genes = [];
        var result = CheckScheme(id);
        if (result != GenomeOperationResult.Success)
            return result;

        foreach (var definition in _prototypes.Index(id).Genes)
        {
            if (!_random.Prob(definition.PresenceChance))
                continue;

            var gene = _prototypes.Index(definition.Gene);
            var state = new GeneState();
            switch (gene.Kind)
            {
                case GeneKind.Allele:
                    state.Allele = definition.Randomize
                        ? gene.Alleles[_random.Next(gene.Alleles.Count)]
                        : definition.DefaultAllele ?? gene.Alleles[0];
                    break;
                case GeneKind.Number:
                    state.Number = definition.Randomize
                        ? _random.Next(gene.Minimum, gene.Maximum + 1)
                        : definition.DefaultNumber ?? gene.Minimum;
                    break;
            }

            genes.Add(definition.Gene, state);
        }

        return GenomeOperationResult.Success;
    }

    private static bool ValidDefinition(GenomeGeneDefinition definition, GenePrototype gene)
    {
        if (definition.PresenceChance is < 0f or > 1f || float.IsNaN(definition.PresenceChance))
            return false;

        return gene.Kind switch
        {
            GeneKind.Presence => definition.DefaultAllele is null && definition.DefaultNumber is null,
            GeneKind.Allele => gene.Alleles.Count > 0 &&
                               gene.Alleles.Distinct().Count() == gene.Alleles.Count &&
                               definition.DefaultNumber is null &&
                               (definition.DefaultAllele is null || gene.Alleles.Contains(definition.DefaultAllele)),
            GeneKind.Number => gene.Minimum <= gene.Maximum && gene.Maximum < int.MaxValue &&
                               definition.DefaultAllele is null &&
                               (definition.DefaultNumber is null ||
                                definition.DefaultNumber >= gene.Minimum && definition.DefaultNumber <= gene.Maximum),
            _ => false
        };
    }

    private GenomeOperationResult GetComponent(EntityUid uid, out GenomeComponent? component)
    {
        if (!TryComp(uid, out component))
            return GenomeOperationResult.MissingGenome;
        return component.GenomeGenerated ? GenomeOperationResult.Success : GenomeOperationResult.Uninitialized;
    }

    public bool TryGetSnapshot(EntityUid uid, out GenomeSnapshot? snapshot)
    {
        snapshot = null;
        if (GetComponent(uid, out var component) != GenomeOperationResult.Success)
            return false;

        snapshot = new GenomeSnapshot(component!.GenomeId, component.Genes);
        return true;
    }

    public bool HasGene(EntityUid uid, ProtoId<GenePrototype> id) =>
        GetComponent(uid, out var component) == GenomeOperationResult.Success && component!.Genes.ContainsKey(id);

    public GenomeOperationResult SetGene(EntityUid uid, ProtoId<GenePrototype> id, GeneState state)
    {
        var result = GetComponent(uid, out var component);
        if (result != GenomeOperationResult.Success)
            return result;
        if (!_prototypes.TryIndex(id, out var gene))
            return GenomeOperationResult.UnknownGene;
        if (!ValidState(state, gene))
            return GenomeOperationResult.InvalidValue;
        var hadOld = component!.Genes.TryGetValue(id, out var old);
        if (hadOld && SameState(old!, state))
            return GenomeOperationResult.Success;

        component.Genes[id] = state.Clone();
        RaiseLocalEvent(uid, new GenomeChangedEvent());
        return GenomeOperationResult.Success;
    }

    public GenomeOperationResult RemoveGene(EntityUid uid, ProtoId<GenePrototype> id)
    {
        var result = GetComponent(uid, out var component);
        if (result != GenomeOperationResult.Success)
            return result;
        if (!_prototypes.HasIndex(id))
            return GenomeOperationResult.UnknownGene;
        if (!component!.Genes.Remove(id))
            return GenomeOperationResult.Success;

        RaiseLocalEvent(uid, new GenomeChangedEvent());
        return GenomeOperationResult.Success;
    }

    private static bool ValidState(GeneState state, GenePrototype gene) => gene.Kind switch
    {
        GeneKind.Presence => state.Allele is null && state.Number is null,
        GeneKind.Allele => state.Number is null && state.Allele is not null && gene.Alleles.Contains(state.Allele),
        GeneKind.Number => state.Allele is null && state.Number is { } value &&
                           value >= gene.Minimum && value <= gene.Maximum,
        _ => false
    };

    private static bool SameState(GeneState first, GeneState second) =>
        first.Allele == second.Allele && first.Number == second.Number;

    private GenomeOperationResult ReplaceGenome(EntityUid uid, ProtoId<GenomePrototype> id,
        Dictionary<ProtoId<GenePrototype>, GeneState> genes)
    {
        if (!TryComp<GenomeComponent>(uid, out var component))
            return GenomeOperationResult.MissingGenome;
        if (component.GenomeId != id)
            return GenomeOperationResult.IncompatibleScheme;
        var result = ValidateGenome(id, genes);
        if (result != GenomeOperationResult.Success)
            return result;

        if (component.GenomeGenerated && component.Genes.Count == genes.Count &&
            component.Genes.All(entry => genes.TryGetValue(entry.Key, out var state) && SameState(entry.Value, state)))
            return GenomeOperationResult.Success;

        Store((uid, component), genes);
        return GenomeOperationResult.Success;
    }

    private void Store(Entity<GenomeComponent> ent, Dictionary<ProtoId<GenePrototype>, GeneState> genes)
    {
        var initialized = ent.Comp.GenomeGenerated;
        ent.Comp.Genes = genes;
        ent.Comp.GenomeGenerated = true;
        if (initialized)
            RaiseLocalEvent(ent.Owner, new GenomeChangedEvent());
        else
            RaiseLocalEvent(ent.Owner, new GenomeReadyEvent());
    }

    /// <summary>
    /// Validates and merges incoming genes into an existing organism genome.
    /// Reports changed genes only after the complete replacement succeeds.
    /// </summary>
    public GenomeOperationResult ApplyGenes(EntityUid uid,
        IReadOnlyDictionary<ProtoId<GenePrototype>, GeneState> incoming, out int changed)
    {
        changed = 0;
        var result = GetComponent(uid, out var component);
        if (result != GenomeOperationResult.Success)
            return result;

        result = ValidateGenes(incoming);
        if (result != GenomeOperationResult.Success)
            return result;

        var genes = component!.Genes.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());
        var count = 0;
        foreach (var (id, state) in incoming)
        {
            if (!genes.TryGetValue(id, out var original) || !SameState(original, state))
                count++;
            genes[id] = state.Clone();
        }

        result = ReplaceGenome(uid, component.GenomeId, genes);
        if (result == GenomeOperationResult.Success)
            changed = count;
        return result;
    }

}
