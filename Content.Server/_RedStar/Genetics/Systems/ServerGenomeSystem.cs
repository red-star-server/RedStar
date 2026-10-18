using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Cloning.Events;
using Content.Server._RedStar.Genetics.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared._RedStar.Genetics.Data;
using Content.Shared._RedStar.Genetics.Events;
using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared._RedStar.Genetics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics.Systems;

public sealed partial class ServerGenomeSystem : GenomeSystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private GeneExpressionSystem _expressions = default!;

    [SubscribeLocalEvent]
    private void OnCloning(Entity<GenomeComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        var clone = EnsureComp<GenomeComponent>(args.CloneUid);
        StopExpressions((args.CloneUid, clone));
        clone.GenomeId = ent.Comp.GenomeId;
        clone.MaximumLoad = ent.Comp.MaximumLoad;
        clone.GenomeGenerated = ent.Comp.GenomeGenerated;
        clone.Genes = new Dictionary<ProtoId<GenePrototype>, GeneStatus>(ent.Comp.Genes);

        if (TryComp<GeneContributionsComponent>(ent, out var contributions))
        {
            foreach (var (name, contribution) in contributions.Components)
            {
                if (!contribution.Owned || !args.Settings.Components.Contains(name) ||
                    MetaData(args.CloneUid).EntityPrototype?.Components.ContainsKey(name) == true)
                    continue;
                if (TryComp(args.CloneUid, Factory.GetRegistration(name).Type, out var copied))
                    RemComp(args.CloneUid, copied);
            }
        }
        RestoreExpressions((args.CloneUid, clone));
        Dirty(args.CloneUid, clone);
        NotifyChanged(args.CloneUid);
    }

    private void NotifyChanged(EntityUid uid)
    {
        var changed = new GenomeChangedEvent();
        RaiseLocalEvent(uid, ref changed);
    }

    private bool TryExpress(EntityUid uid, ProtoId<GenePrototype> id, bool firstActivation)
    {
        if (!IsGeneCompatible(uid, id))
            return false;

        var gene = _prototypes.Index(id);
        if (!_expressions.TryStart(uid, gene))
            return false;

        if (!firstActivation || gene.Effects.Length == 0 || _effects.TryApplyEffects(uid, gene.Effects))
            return true;

        _expressions.Stop(uid, gene);
        return false;
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GenomeComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.GenomeGenerated)
        {
            if (!_prototypes.HasIndex(ent.Comp.GenomeId))
                return;
            ent.Comp.GenomeGenerated = true;
            Dirty(ent);
        }
        RestoreExpressions(ent);
        NotifyChanged(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GenomeComponent> ent, ref ComponentShutdown args) => StopExpressions(ent);

    [SubscribeLocalEvent]
    private void OnExpressionEnded(Entity<GenomeComponent> ent, ref GeneExpressionEndedEvent args)
    {
        if (!ent.Comp.Genes.TryGetValue(args.Gene, out var state) || state != GeneStatus.Active)
            return;
        ent.Comp.Genes[args.Gene] = GeneStatus.Suppressed;
        NotifyChanged(ent.Owner);
    }

    private void RestoreExpressions(Entity<GenomeComponent> ent)
    {
        foreach (var id in ent.Comp.Genes.Where(gene => gene.Value == GeneStatus.Active).Select(gene => gene.Key).ToArray())
        {
            if (!TryExpress(ent.Owner, id, firstActivation: false))
                ent.Comp.Genes[id] = GeneStatus.Suppressed;
        }
    }

    private void StopExpressions(Entity<GenomeComponent> ent)
    {
        foreach (var id in ent.Comp.Genes.Keys.ToArray())
        {
            if (_prototypes.TryIndex(id, out var gene))
                _expressions.Stop(ent.Owner, gene);
        }
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
    /// Copies all present genes, including dormant and suppressed ones, without their expression status.
    /// Returns false if the entity has no initialized genome.
    /// </summary>
    public bool TryGetSnapshot(Entity<GenomeComponent?> ent, [NotNullWhen(true)] out GenomeSnapshot? snapshot)
    {
        snapshot = null;
        if (!Resolve(ent, ref ent.Comp, logMissing: false) || !ent.Comp.GenomeGenerated)
            return false;
        snapshot = new GenomeSnapshot(ent.Comp.GenomeId,
            _prototypes.Index(ent.Comp.GenomeId).Genes.Union(ent.Comp.Genes.Keys));
        return true;
    }

    /// <summary>
    /// Checks for an acquired modification regardless of its expression state. Catalogue entries are excluded.
    /// </summary>
    public bool HasGene(Entity<GenomeComponent?> ent, ProtoId<GenePrototype> id) =>
        Resolve(ent, ref ent.Comp, logMissing: false) && ent.Comp.Genes.ContainsKey(id);

    /// <summary>
    /// Checks whether the gene is present but has not been expressed.
    /// </summary>
    public bool IsRaw(Entity<GenomeComponent?> ent, ProtoId<GenePrototype> id) =>
        Resolve(ent, ref ent.Comp, logMissing: false) &&
        ent.Comp.Genes.TryGetValue(id, out var status) && status == GeneStatus.Raw;

    /// <summary>
    /// Checks the gene's organism whitelist and blacklist. Returns false for an unknown gene.
    /// Does not check expression state or remaining acquired load.
    /// </summary>
    public bool IsGeneCompatible(EntityUid uid, ProtoId<GenePrototype> id) =>
        _prototypes.TryIndex(id, out var gene) && _whitelist.CheckBoth(uid, gene.Blacklist, gene.Whitelist);

    private int Load(IEnumerable<ProtoId<GenePrototype>> genes) =>
        genes.Sum(id => _prototypes.Index(id).LoadCost);

    /// <summary>
    /// Checks that incoming genes are compatible and their combined acquired load fits the limit.
    /// Acquired genes are counted once; donor catalogue entries do not consume load.
    /// </summary>
    public bool CanApplyGenes(Entity<GenomeComponent?> ent, IReadOnlySet<ProtoId<GenePrototype>> incoming) =>
        Resolve(ent, ref ent.Comp, logMissing: false) && ent.Comp.GenomeGenerated &&
        incoming.All(id => IsGeneCompatible(ent.Owner, id)) &&
        Load(ent.Comp.Genes.Keys.Union(incoming)) <= ent.Comp.MaximumLoad;

    /// <summary>
    /// Acquired genes remain raw until activated. Existing genes retain their expression state.
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
    /// Expresses compatible dormant genes once. Suppressed genes remain suppressed; load is unchanged.
    /// </summary>
    public void ActivateGenes(Entity<GenomeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false) || !ent.Comp.GenomeGenerated)
            return;

        var raw = ent.Comp.Genes.Where(gene => gene.Value == GeneStatus.Raw).Select(gene => gene.Key).ToArray();
        var changed = false;
        foreach (var id in raw)
        {
            if (!TryExpress(ent.Owner, id, firstActivation: true))
                continue;
            ent.Comp.Genes[id] = GeneStatus.Active;
            changed = true;
        }
        if (!changed)
            return;

        NotifyChanged(ent.Owner);
    }

    /// <summary>
    /// Disables an active acquired gene without removing its genetic material or refunding reserved load.
    /// </summary>
    public bool TrySuppressGene(Entity<GenomeComponent?> ent, ProtoId<GenePrototype> id)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false) ||
            !ent.Comp.Genes.TryGetValue(id, out var state) || state != GeneStatus.Active)
            return false;

        ent.Comp.Genes[id] = GeneStatus.Suppressed;
        _expressions.Stop(ent.Owner, _prototypes.Index(id));
        NotifyChanged(ent.Owner);
        return true;
    }

    /// <summary>
    /// Restores a suppressed gene's persistent manifestation without repeating its one-time activation effects.
    /// </summary>
    public bool TryResumeGene(Entity<GenomeComponent?> ent, ProtoId<GenePrototype> id)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false) ||
            !ent.Comp.Genes.TryGetValue(id, out var state) || state != GeneStatus.Suppressed ||
            !TryExpress(ent.Owner, id, firstActivation: false))
            return false;

        ent.Comp.Genes[id] = GeneStatus.Active;
        NotifyChanged(ent.Owner);
        return true;
    }

    /// <summary>
    /// Removes an acquired gene and its manifestation, releasing its reserved load. The donor catalogue is unchanged.
    /// </summary>
    public bool TryRemoveGene(Entity<GenomeComponent?> ent, ProtoId<GenePrototype> id)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false) || !ent.Comp.Genes.Remove(id))
            return false;

        _expressions.Stop(ent.Owner, _prototypes.Index(id));
        NotifyChanged(ent.Owner);
        return true;
    }

    /// <summary>
    /// Checks that inputs are dormant, the result is absent and compatible, and the resulting load fits.
    /// Does not consume inputs or apply gene effects.
    /// </summary>
    public bool CanTransformGenes(Entity<GenomeComponent?> ent, IReadOnlySet<ProtoId<GenePrototype>> inputs,
        ProtoId<GenePrototype> result) =>
        inputs.Count > 0 && !inputs.Contains(result) && IsGeneCompatible(ent.Owner, result) &&
        Resolve(ent, ref ent.Comp, logMissing: false) && ent.Comp.GenomeGenerated &&
        !ent.Comp.Genes.ContainsKey(result) &&
        inputs.All(id => ent.Comp.Genes.TryGetValue(id, out var status) && status == GeneStatus.Raw) &&
        Load(ent.Comp.Genes.Keys.Except(inputs).Append(result)) <= ent.Comp.MaximumLoad;

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

        var expressed = TryExpress(ent.Owner, result, firstActivation: true);
        ent.Comp.Genes.Add(result, expressed ? GeneStatus.Active : GeneStatus.Raw);
        NotifyChanged(ent.Owner);
        return true;
    }
}
