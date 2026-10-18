using System.Linq;
using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Cloning.Events;

namespace Content.Server._RedStar.Genetics;

public sealed partial class ServerGenomeSystem : GenomeSystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private GenomeExpressionSystem _expression = default!;
    [Dependency] private SharedContainerSystem _containers = default!;

    [SubscribeLocalEvent]
    private void OnCloning(Entity<GenomeComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        CopyComp(ent.Owner, args.CloneUid, ent.Comp);
        var clone = Comp<GenomeComponent>(args.CloneUid);
        var acquired = clone.Genes.Where(gene => gene.Value == GeneStatus.Active &&
            !clone.NaturalGenes.Contains(gene.Key)).Select(gene => gene.Key).ToArray();
        foreach (var id in acquired)
        {
            if (!_expression.TryExpress(args.CloneUid, id))
                clone.Genes[id] = GeneStatus.Raw;
        }
        NotifyChanged(args.CloneUid, acquired.Where(id => clone.Genes[id] == GeneStatus.Active).ToArray());
    }

    private void NotifyChanged(EntityUid uid, IReadOnlyList<ProtoId<GenePrototype>> activated)
    {
        var changed = new GenomeChangedEvent(activated);
        RaiseLocalEvent(uid, ref changed);
        if (!_containers.TryGetContainingContainer(uid, out var container))
            return;

        var ev = new ContainedGenomeChangedEvent(uid);
        RaiseLocalEvent(container.Owner, ref ev);
    }

    [SubscribeLocalEvent(after: [typeof(SharedSolutionContainerSystem)])]
    private void OnMapInit(Entity<GenomeComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.GenomeGenerated)
            return;

        if (!TryGenerate(ent.Comp.GenomeId, out var genome) || genome is null)
            return;

        ent.Comp.Genes = genome.Genes.ToDictionary(id => id, _ => GeneStatus.Active);
        ent.Comp.NaturalGenes = new HashSet<ProtoId<GenePrototype>>(genome.Genes);
        ent.Comp.GenomeGenerated = true;
        Dirty(ent);
        NotifyChanged(ent.Owner, []);
    }

    /// <summary>
    /// Builds an independent snapshot of the natural genes in a genome prototype.
    /// Returns false for an unknown prototype.
    /// </summary>
    public bool TryGenerate(ProtoId<GenomePrototype> id, out GenomeSnapshot? genome)
    {
        genome = null;
        if (!_prototypes.TryIndex(id, out var scheme))
            return false;
        genome = new GenomeSnapshot(id, scheme.Genes.Select(g => g.Gene));
        return true;
    }

    /// <summary>
    /// Copies all present genes, including dormant ones, without their expression status.
    /// Returns false if the entity has no initialized genome.
    /// </summary>
    public bool TryGetSnapshot(EntityUid uid, out GenomeSnapshot? snapshot)
    {
        snapshot = null;
        if (!TryComp<GenomeComponent>(uid, out var component) || !component.GenomeGenerated)
            return false;
        snapshot = new GenomeSnapshot(component.GenomeId, component.Genes.Keys);
        return true;
    }

    /// <summary>
    /// Checks for a present gene regardless of whether it is dormant or active.
    /// </summary>
    public bool HasGene(EntityUid uid, ProtoId<GenePrototype> id) =>
        TryComp<GenomeComponent>(uid, out var component) && component.Genes.ContainsKey(id);

    /// <summary>
    /// Checks whether the gene is present but has not been expressed.
    /// </summary>
    public bool IsRaw(EntityUid uid, ProtoId<GenePrototype> id) =>
        TryComp<GenomeComponent>(uid, out var component) &&
        component.Genes.TryGetValue(id, out var status) && status == GeneStatus.Raw;

    private int Load(GenomeComponent component, IEnumerable<ProtoId<GenePrototype>> genes) =>
        genes.Where(id => !component.NaturalGenes.Contains(id)).Sum(id => _prototypes.Index(id).Tier);

    /// <summary>
    /// Checks that all incoming genes exist and their combined acquired load fits the limit.
    /// Genes already present are counted once; natural genes do not consume acquired load.
    /// </summary>
    public bool CanApplyGenes(EntityUid uid, IReadOnlySet<ProtoId<GenePrototype>> incoming) =>
        TryComp<GenomeComponent>(uid, out var component) && component.GenomeGenerated &&
        incoming.All(id => _prototypes.HasIndex(id)) &&
        Load(component, component.Genes.Keys.Union(incoming)) <= component.MaximumLoad;

    /// <summary>
    /// Acquired genes remain raw until activated. Existing active genes are preserved.
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

    /// <summary>
    /// Expresses dormant genes once without adding genetic material or changing its load.
    /// </summary>
    public void ActivateGenes(EntityUid uid)
    {
        if (!TryComp<GenomeComponent>(uid, out var component) || !component.GenomeGenerated)
            return;

        var raw = component.Genes.Where(gene => gene.Value == GeneStatus.Raw).Select(gene => gene.Key).ToArray();
        var activated = new List<ProtoId<GenePrototype>>();
        foreach (var id in raw)
        {
            if (!_expression.TryExpress(uid, id))
                continue;
            component.Genes[id] = GeneStatus.Active;
            activated.Add(id);
        }
        if (activated.Count == 0)
            return;

        NotifyChanged(uid, activated);
    }

    /// <summary>
    /// Checks that every input is dormant, the result is absent, and the resulting load fits.
    /// Does not consume inputs or apply gene effects.
    /// </summary>
    public bool CanTransformGenes(EntityUid uid, IReadOnlySet<ProtoId<GenePrototype>> inputs,
        ProtoId<GenePrototype> result) =>
        inputs.Count > 0 && !inputs.Contains(result) && _prototypes.HasIndex(result) &&
        TryComp<GenomeComponent>(uid, out var component) && component.GenomeGenerated &&
        !component.Genes.ContainsKey(result) && inputs.All(id => IsRaw(uid, id)) &&
        Load(component, component.Genes.Keys.Except(inputs).Append(result)) <= component.MaximumLoad;

    /// <summary>
    /// Consumes the dormant input genes and adds the result, expressing it immediately.
    /// If no effect passes its conditions, the result remains dormant; inputs are still consumed.
    /// </summary>
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

        var expressed = _expression.TryExpress(uid, result);
        component.Genes.Add(result, expressed ? GeneStatus.Active : GeneStatus.Raw);
        NotifyChanged(uid, expressed ? [result] : []);
        return true;
    }
}
