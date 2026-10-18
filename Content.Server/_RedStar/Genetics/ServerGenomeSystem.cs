using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.Cloning.Events;

namespace Content.Server._RedStar.Genetics;

public sealed partial class ServerGenomeSystem : GenomeSystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

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
            if (!TryExpress(args.CloneUid, id))
                clone.Genes[id] = GeneStatus.Raw;
        }
        NotifyChanged(args.CloneUid);
    }

    private void NotifyChanged(EntityUid uid)
    {
        var changed = new GenomeChangedEvent();
        RaiseLocalEvent(uid, ref changed);
    }

    /// <summary>
    /// Applies the gene's effects. Success means at least one effect passed its conditions,
    /// or the gene has no effects; it does not guarantee that every effect was applied.
    /// </summary>
    private bool TryExpress(EntityUid uid, ProtoId<GenePrototype> id)
    {
        var effects = _prototypes.Index(id).Effects;
        if (effects.Length == 0 || _effects.TryApplyEffects(uid, effects))
            return true;

        Log.Warning($"Could not express gene {id} on {ToPrettyString(uid)}.");
        return false;
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GenomeComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.GenomeGenerated)
            return;

        if (!TryGenerate(ent.Comp.GenomeId, out var genome))
            return;

        ent.Comp.Genes = genome.Genes.ToDictionary(id => id, _ => GeneStatus.Active);
        ent.Comp.NaturalGenes = new HashSet<ProtoId<GenePrototype>>(genome.Genes);
        ent.Comp.GenomeGenerated = true;
        Dirty(ent);
        NotifyChanged(ent.Owner);
    }

    /// <summary>
    /// Builds an independent snapshot of the natural genes in a genome prototype.
    /// Returns false for an unknown prototype.
    /// </summary>
    public bool TryGenerate(ProtoId<GenomePrototype> id, [NotNullWhen(true)] out GenomeSnapshot? genome)
    {
        genome = null;
        if (!_prototypes.TryIndex(id, out var scheme))
            return false;
        genome = new GenomeSnapshot(id, scheme.Genes);
        return true;
    }

    /// <summary>
    /// Copies all present genes, including dormant ones, without their expression status.
    /// Returns false if the entity has no initialized genome.
    /// </summary>
    public bool TryGetSnapshot(Entity<GenomeComponent?> ent, [NotNullWhen(true)] out GenomeSnapshot? snapshot)
    {
        snapshot = null;
        if (!Resolve(ent, ref ent.Comp, logMissing: false) || !ent.Comp.GenomeGenerated)
            return false;
        snapshot = new GenomeSnapshot(ent.Comp.GenomeId, ent.Comp.Genes.Keys);
        return true;
    }

    /// <summary>
    /// Checks for a present gene regardless of whether it is dormant or active.
    /// </summary>
    public bool HasGene(Entity<GenomeComponent?> ent, ProtoId<GenePrototype> id) =>
        Resolve(ent, ref ent.Comp, logMissing: false) && ent.Comp.Genes.ContainsKey(id);

    /// <summary>
    /// Checks whether the gene is present but has not been expressed.
    /// </summary>
    public bool IsRaw(Entity<GenomeComponent?> ent, ProtoId<GenePrototype> id) =>
        Resolve(ent, ref ent.Comp, logMissing: false) &&
        ent.Comp.Genes.TryGetValue(id, out var status) && status == GeneStatus.Raw;

    private int Load(GenomeComponent component, IEnumerable<ProtoId<GenePrototype>> genes) =>
        genes.Where(id => !component.NaturalGenes.Contains(id)).Sum(id => _prototypes.Index(id).Tier);

    /// <summary>
    /// Checks that all incoming genes exist and their combined acquired load fits the limit.
    /// Genes already present are counted once; natural genes do not consume acquired load.
    /// </summary>
    public bool CanApplyGenes(Entity<GenomeComponent?> ent, IReadOnlySet<ProtoId<GenePrototype>> incoming) =>
        Resolve(ent, ref ent.Comp, logMissing: false) && ent.Comp.GenomeGenerated &&
        incoming.All(id => _prototypes.HasIndex(id)) &&
        Load(ent.Comp, ent.Comp.Genes.Keys.Union(incoming)) <= ent.Comp.MaximumLoad;

    /// <summary>
    /// Acquired genes remain raw until activated. Existing active genes are preserved.
    /// </summary>
    public bool TryApplyGenes(Entity<GenomeComponent?> ent, IReadOnlySet<ProtoId<GenePrototype>> incoming, out int changed)
    {
        changed = 0;
        if (!Resolve(ent, ref ent.Comp, logMissing: false) || !CanApplyGenes(ent, incoming))
            return false;
        foreach (var id in incoming)
        {
            if (ent.Comp.Genes.TryAdd(id, GeneStatus.Raw))
                changed++;
        }
        if (changed > 0)
            NotifyChanged(ent.Owner);
        return true;
    }

    /// <summary>
    /// Expresses dormant genes once without adding genetic material or changing its load.
    /// </summary>
    public void ActivateGenes(Entity<GenomeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false) || !ent.Comp.GenomeGenerated)
            return;

        var raw = ent.Comp.Genes.Where(gene => gene.Value == GeneStatus.Raw).Select(gene => gene.Key).ToArray();
        var changed = false;
        foreach (var id in raw)
        {
            if (!TryExpress(ent.Owner, id))
                continue;
            ent.Comp.Genes[id] = GeneStatus.Active;
            changed = true;
        }
        if (!changed)
            return;

        NotifyChanged(ent.Owner);
    }

    /// <summary>
    /// Checks that every input is dormant, the result is absent, and the resulting load fits.
    /// Does not consume inputs or apply gene effects.
    /// </summary>
    public bool CanTransformGenes(Entity<GenomeComponent?> ent, IReadOnlySet<ProtoId<GenePrototype>> inputs,
        ProtoId<GenePrototype> result) =>
        inputs.Count > 0 && !inputs.Contains(result) && _prototypes.HasIndex(result) &&
        Resolve(ent, ref ent.Comp, logMissing: false) && ent.Comp.GenomeGenerated &&
        !ent.Comp.Genes.ContainsKey(result) &&
        inputs.All(id => ent.Comp.Genes.TryGetValue(id, out var status) && status == GeneStatus.Raw) &&
        Load(ent.Comp, ent.Comp.Genes.Keys.Except(inputs).Append(result)) <= ent.Comp.MaximumLoad;

    /// <summary>
    /// Consumes the dormant input genes and adds the result, expressing it immediately.
    /// If no effect passes its conditions, the result remains dormant; inputs are still consumed.
    /// </summary>
    public bool TryTransformGenes(Entity<GenomeComponent?> ent, IReadOnlySet<ProtoId<GenePrototype>> inputs,
        ProtoId<GenePrototype> result)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false) || !CanTransformGenes(ent, inputs, result))
            return false;
        foreach (var id in inputs)
        {
            ent.Comp.Genes.Remove(id);
        }

        var expressed = TryExpress(ent.Owner, result);
        ent.Comp.Genes.Add(result, expressed ? GeneStatus.Active : GeneStatus.Raw);
        NotifyChanged(ent.Owner);
        return true;
    }
}
