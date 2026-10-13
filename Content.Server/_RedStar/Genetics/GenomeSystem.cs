using System.Linq;
using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Owns generation, inheritance and all mutations of an organism's genome.
/// </summary>
public sealed partial class GenomeSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IRobustRandom _random = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GenomeComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Initialized)
            return;

        var result = Generate(ent.Comp.GenomeId, out var genome);
        if (result != GenomeOperationResult.Success || genome is null)
        {
            Log.Error($"Could not generate genome {ent.Comp.GenomeId} for {ToPrettyString(ent)}: {result}");
            return;
        }

        Store(ent, genome, true);
    }

    public GenomeOperationResult Generate(ProtoId<GenomePrototype> id, out GenomeSnapshot? genome)
    {
        genome = null;
        if (!_prototypes.TryIndex(id, out var scheme))
            return GenomeOperationResult.UnknownScheme;

        var genes = new Dictionary<ProtoId<GenePrototype>, GeneState>();
        foreach (var definition in scheme.Genes)
        {
            if (!_prototypes.TryIndex(definition.Gene, out var gene))
                return GenomeOperationResult.UnknownGene;
            if (genes.ContainsKey(definition.Gene) || !ValidDefinition(definition, gene))
                return GenomeOperationResult.InvalidValue;

            var state = new GeneState { Present = _random.Prob(definition.PresenceChance) };
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

        genome = new GenomeSnapshot(id, genes);
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

    public bool TryGetSnapshot(EntityUid uid, out GenomeSnapshot? snapshot)
    {
        snapshot = null;
        if (!TryComp<GenomeComponent>(uid, out var component) || !component.Initialized)
            return false;

        snapshot = new GenomeSnapshot(component.GenomeId, component.Genes);
        return true;
    }

    public bool TryGetGene(EntityUid uid, ProtoId<GenePrototype> gene, out GeneState? state)
    {
        state = null;
        if (!TryComp<GenomeComponent>(uid, out var component) || !component.Initialized ||
            !component.Genes.TryGetValue(gene, out var stored))
            return false;

        state = stored.Clone();
        return true;
    }

    public bool HasGene(EntityUid uid, ProtoId<GenePrototype> gene) =>
        TryGetGene(uid, gene, out var state) && state!.Present;

    public GenomeOperationResult SetGene(EntityUid uid, ProtoId<GenePrototype> id, GeneState state)
    {
        if (!TryComp<GenomeComponent>(uid, out var component))
            return GenomeOperationResult.MissingGenome;
        if (!component.Initialized)
            return GenomeOperationResult.Uninitialized;
        if (!_prototypes.TryIndex(component.GenomeId, out var scheme))
            return GenomeOperationResult.UnknownScheme;
        if (!_prototypes.TryIndex(id, out var gene) || scheme.Genes.All(definition => definition.Gene != id))
            return GenomeOperationResult.UnknownGene;
        if (!ValidState(state, gene))
            return GenomeOperationResult.InvalidValue;

        var existed = component.Genes.TryGetValue(id, out var old);
        if (existed && old!.Present == state.Present && old.Allele == state.Allele && old.Number == state.Number)
            return GenomeOperationResult.Success;

        component.Genes[id] = state.Clone();
        var kind = !existed || !old!.Present && state.Present
            ? GenomeChangeKind.Added
            : old.Present && !state.Present ? GenomeChangeKind.Removed : GenomeChangeKind.Changed;
        RaiseLocalEvent(uid, new GenomeChangedEvent(kind, id));
        return GenomeOperationResult.Success;
    }

    public GenomeOperationResult RemoveGene(EntityUid uid, ProtoId<GenePrototype> id)
    {
        if (!TryComp<GenomeComponent>(uid, out var component))
            return GenomeOperationResult.MissingGenome;
        if (!component.Initialized)
            return GenomeOperationResult.Uninitialized;
        if (!TryGetGene(uid, id, out var state))
            return GenomeOperationResult.UnknownGene;
        state!.Present = false;
        return SetGene(uid, id, state);
    }

    private static bool ValidState(GeneState state, GenePrototype gene) => gene.Kind switch
    {
        GeneKind.Presence => state.Allele is null && state.Number is null,
        GeneKind.Allele => state.Number is null && state.Allele is not null && gene.Alleles.Contains(state.Allele),
        GeneKind.Number => state.Allele is null && state.Number is { } value &&
                           value >= gene.Minimum && value <= gene.Maximum,
        _ => false
    };

    public GenomeOperationResult ReplaceGenome(EntityUid uid, GenomeSnapshot snapshot)
    {
        if (!TryComp<GenomeComponent>(uid, out var component))
            return GenomeOperationResult.MissingGenome;
        if (!Compatible(component.GenomeId, snapshot.GenomeId))
            return GenomeOperationResult.IncompatibleScheme;
        if (!_prototypes.TryIndex(component.GenomeId, out var target))
            return GenomeOperationResult.UnknownScheme;

        var incoming = snapshot.GetGenes();
        if (incoming.Count != target.Genes.Count)
            return GenomeOperationResult.InvalidValue;
        foreach (var definition in target.Genes)
        {
            if (!incoming.TryGetValue(definition.Gene, out var state) ||
                !_prototypes.TryIndex(definition.Gene, out var gene) || !ValidState(state, gene))
                return GenomeOperationResult.InvalidValue;
        }

        Store((uid, component), snapshot, true);
        return GenomeOperationResult.Success;
    }

    private void Store(Entity<GenomeComponent> ent, GenomeSnapshot snapshot, bool notify)
    {
        ent.Comp.Genes = snapshot.GetGenes().ToDictionary(entry => entry.Key, entry => entry.Value);
        ent.Comp.Initialized = true;
        if (notify)
            RaiseLocalEvent(ent.Owner, new GenomeChangedEvent(GenomeChangeKind.Replaced));
    }

    public bool Compatible(ProtoId<GenomePrototype> first, ProtoId<GenomePrototype> second)
    {
        if (!_prototypes.TryIndex(first, out var a) || !_prototypes.TryIndex(second, out var b))
            return false;
        return first == second || a.Compatibility is { Length: > 0 } key && key == b.Compatibility;
    }

    public GenomeOperationResult CopyGenome(EntityUid source, EntityUid target)
    {
        if (!TryComp<GenomeComponent>(source, out var component))
            return GenomeOperationResult.MissingGenome;
        if (!component.Initialized)
            return GenomeOperationResult.Uninitialized;
        return !TryGetSnapshot(source, out var snapshot) ? GenomeOperationResult.MissingGenome : ReplaceGenome(target, snapshot!);
    }

    public GenomeOperationResult CreateOffspring(GenomeSnapshot first, GenomeSnapshot second,
        ProtoId<GenomePrototype> childId, out GenomeSnapshot? child)
    {
        child = null;
        if (!_prototypes.HasIndex(first.GenomeId) || !_prototypes.HasIndex(second.GenomeId) ||
            !_prototypes.HasIndex(childId))
            return GenomeOperationResult.UnknownScheme;
        if (!Compatible(first.GenomeId, second.GenomeId) || !Compatible(first.GenomeId, childId))
            return GenomeOperationResult.IncompatibleScheme;
        var result = Generate(childId, out var generated);
        if (result != GenomeOperationResult.Success)
            return result;

        var genes = generated!.GetGenes().ToDictionary(entry => entry.Key, entry => entry.Value);
        foreach (var id in genes.Keys.ToArray())
        {
            if (first.TryGetGene(id, out var a) && second.TryGetGene(id, out var b))
                genes[id] = (_random.Prob(0.5f) ? a : b)!.Clone();
        }

        child = new GenomeSnapshot(childId, genes);
        return GenomeOperationResult.Success;
    }

    public GenomeOperationResult MutateGene(EntityUid uid, ProtoId<GenePrototype> id)
    {
        if (!TryComp<GenomeComponent>(uid, out var component))
            return GenomeOperationResult.MissingGenome;
        if (!component.Initialized)
            return GenomeOperationResult.Uninitialized;
        if (!TryGetGene(uid, id, out var state) || !_prototypes.TryIndex(id, out var gene))
            return GenomeOperationResult.UnknownGene;

        switch (gene.Kind)
        {
            case GeneKind.Presence:
                state!.Present = !state.Present;
                break;
            case GeneKind.Allele:
                var alternatives = gene.Alleles.Where(allele => allele != state!.Allele).ToArray();
                if (alternatives.Length == 0)
                    return GenomeOperationResult.InvalidValue;
                state!.Allele = alternatives[_random.Next(alternatives.Length)];
                break;
            case GeneKind.Number:
                if (gene.Minimum == gene.Maximum)
                    return GenomeOperationResult.InvalidValue;
                var value = state!.Number!.Value;
                state.Number = value == gene.Minimum ? value + 1 : value == gene.Maximum ? value - 1 :
                    value + (_random.Prob(0.5f) ? 1 : -1);
                break;
        }

        return SetGene(uid, id, state!);
    }
}
