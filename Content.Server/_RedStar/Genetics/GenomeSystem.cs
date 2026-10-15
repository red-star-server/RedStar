using System.Linq;
using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Containers;

namespace Content.Server._RedStar.Genetics;

public sealed partial class GenomeSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedContainerSystem _containers = default!;

    private void NotifyChanged(EntityUid uid, IReadOnlyList<ProtoId<GenePrototype>> activated)
    {
        RaiseLocalEvent(uid, new GenomeChangedEvent(activated));
        if (!_containers.TryGetContainingContainer(uid, out var container))
            return;

        var ev = new ContainedGenomeChangedEvent(uid);
        RaiseLocalEvent(container.Owner, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GenomeComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.GenomeGenerated)
            return;

        if (!TryGenerate(ent.Comp.GenomeId, out var genome) || genome is null)
            return;

        ent.Comp.Genes = genome.Genes.ToDictionary(id => id, _ => GeneStatus.Active);
        ent.Comp.NaturalGenes = new HashSet<ProtoId<GenePrototype>>(genome.Genes);
        ent.Comp.GenomeGenerated = true;
        NotifyChanged(ent.Owner, genome.Genes.ToArray());
    }

    public bool TryGenerate(ProtoId<GenomePrototype> id, out GenomeSnapshot? genome)
    {
        genome = null;
        if (!_prototypes.TryIndex(id, out var scheme))
            return false;
        genome = new GenomeSnapshot(id, scheme.Genes.Where(g => _random.Prob(g.PresenceChance)).Select(g => g.Gene));
        return true;
    }

    public bool TryGetSnapshot(EntityUid uid, out GenomeSnapshot? snapshot)
    {
        snapshot = null;
        if (!TryComp<GenomeComponent>(uid, out var component) || !component.GenomeGenerated)
            return false;
        snapshot = new GenomeSnapshot(component.GenomeId, component.Genes.Keys);
        return true;
    }

    public bool HasGene(EntityUid uid, ProtoId<GenePrototype> id) =>
        TryComp<GenomeComponent>(uid, out var component) && component.Genes.ContainsKey(id);

    public bool IsRaw(EntityUid uid, ProtoId<GenePrototype> id) =>
        TryComp<GenomeComponent>(uid, out var component) &&
        component.Genes.TryGetValue(id, out var status) && status == GeneStatus.Raw;

    private int Load(GenomeComponent component, IEnumerable<ProtoId<GenePrototype>> genes) =>
        genes.Where(id => !component.NaturalGenes.Contains(id)).Sum(id => _prototypes.Index(id).Tier);

    public bool CanApplyGenes(EntityUid uid, IReadOnlySet<ProtoId<GenePrototype>> incoming) =>
        TryComp<GenomeComponent>(uid, out var component) && component.GenomeGenerated &&
        incoming.All(id => _prototypes.HasIndex(id)) &&
        Load(component, component.Genes.Keys.Union(incoming)) <= component.MaximumLoad;

    /// <summary>
    /// Acquired genes remain raw indefinitely. Existing active genes are preserved.
    /// </summary>
    public bool TryApplyGenes(EntityUid uid, IReadOnlySet<ProtoId<GenePrototype>> incoming, out int changed)
    {
        changed = 0;
        if (!CanApplyGenes(uid, incoming))
            return false;
        var component = Comp<GenomeComponent>(uid);
        foreach (var id in incoming)
        {
            if (component.Genes.TryAdd(id, GeneStatus.Raw))
                changed++;
        }
        if (changed > 0)
            NotifyChanged(uid, []);
        return true;
    }

    public bool CanTransformGenes(EntityUid uid, IReadOnlySet<ProtoId<GenePrototype>> inputs,
        ProtoId<GenePrototype> result) =>
        inputs.Count > 0 && !inputs.Contains(result) && _prototypes.HasIndex(result) &&
        TryComp<GenomeComponent>(uid, out var component) && component.GenomeGenerated &&
        !component.Genes.ContainsKey(result) && inputs.All(id => IsRaw(uid, id)) &&
        Load(component, component.Genes.Keys.Except(inputs).Append(result)) <= component.MaximumLoad;

    public bool TryTransformGenes(EntityUid uid, IReadOnlySet<ProtoId<GenePrototype>> inputs,
        ProtoId<GenePrototype> result)
    {
        if (!CanTransformGenes(uid, inputs, result))
            return false;
        var component = Comp<GenomeComponent>(uid);
        foreach (var id in inputs)
        {
            component.Genes.Remove(id);
        }

        component.Genes.Add(result, GeneStatus.Raw);
        NotifyChanged(uid, []);
        return true;
    }
}
