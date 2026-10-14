using Content.Shared._RedStar.Genetics;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Applies each phenotype rule once when its genetic condition first holds.
/// Effects are permanent; replacing or removing genes does not reverse them.
/// </summary>
public sealed partial class GenomeExpressionSystem : EntitySystem
{
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnGenomeChanged(Entity<GenomeComponent> ent, ref GenomeChangedEvent args) => Recalculate(ent.Owner);

    [SubscribeLocalEvent]
    private void OnGenomeReady(Entity<GenomeComponent> ent, ref GenomeReadyEvent args) => Recalculate(ent.Owner);

    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<GenomeExpressionComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Applied = [..ent.Comp.Applied];
    }

    private void Recalculate(EntityUid uid)
    {
        if (!_genomes.TryGetSnapshot(uid, out var genome) || genome is null)
            return;

        GenomeExpressionComponent? host = null;
        foreach (var (id, _) in genome.GetGenes())
        {
            if (!_prototypes.TryIndex(id, out var gene) || gene.Effects.Length == 0)
                continue;

            host ??= EnsureComp<GenomeExpressionComponent>(uid);
            if (host.Applied.Contains(id))
                continue;
            if (_effects.TryApplyEffects(uid, gene.Effects))
                host.Applied.Add(id);
        }
    }
}
