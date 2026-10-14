using System.Linq;
using Content.Shared._RedStar.Genetics;
using Content.Shared.EntityEffects;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Expresses natural genes immediately and incubates later genetic changes.
/// </summary>
public sealed partial class GenomeExpressionSystem : EntitySystem
{
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private IConfigurationManager _config = default!;

    [SubscribeLocalEvent]
    private void OnGenomeChanged(Entity<GenomeComponent> ent, ref GenomeChangedEvent args) =>
        Recalculate(ent.Owner, false);

    [SubscribeLocalEvent]
    private void OnGenomeReady(Entity<GenomeComponent> ent, ref GenomeReadyEvent args) =>
        Recalculate(ent.Owner, true);

    private void Recalculate(EntityUid uid, bool natural)
    {
        if (!_genomes.TryGetSnapshot(uid, out var genome) || genome is null)
            return;

        var host = EnsureComp<GenomeExpressionComponent>(uid);
        natural &= !host.InitialGenomeExpressed;
        host.InitialGenomeExpressed = true;

        var genes = genome.GetGenes();
        foreach (var (id, state) in genes)
        {
            if (!_prototypes.TryIndex(id, out var gene) || gene.Effects.Length == 0)
                continue;

            if (host.Applied.TryGetValue(id, out var applied) && SameValue(applied, state))
            {
                host.Incubating.Remove(id);
                continue;
            }

            if (natural)
            {
                if (_effects.TryApplyEffects(uid, gene.Effects))
                    host.Applied[id] = state.Clone();
                continue;
            }

            if (!host.Incubating.TryGetValue(id, out var pending) || !SameValue(pending.Value, state))
            {
                host.Incubating[id] = new IncubatingGene(state,
                    TimeSpan.FromSeconds(Math.Max(0, _config.GetCVar(GeneticsCVars.IncubationSeconds))));
            }
        }

        foreach (var id in host.Incubating.Keys.ToArray())
        {
            if (!genes.TryGetValue(id, out var state) || !SameValue(host.Incubating[id].Value, state))
                host.Incubating.Remove(id);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GenomeExpressionComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var host, out var metadata))
        {
            if (metadata.EntityPaused || host.Incubating.Count == 0)
                continue;

            foreach (var (id, pending) in host.Incubating.ToArray())
            {
                pending.Remaining -= TimeSpan.FromSeconds(frameTime);
                if (pending.Remaining > TimeSpan.Zero)
                    continue;

                if (!_genomes.TryGetGene(uid, id, out var current) || current is null ||
                    !SameValue(current, pending.Value) ||
                    !_prototypes.TryIndex(id, out var gene) || gene.Effects.Length == 0)
                {
                    host.Incubating.Remove(id);
                    continue;
                }

                if (_effects.TryApplyEffects(uid, gene.Effects))
                {
                    host.Applied[id] = current;
                    host.Incubating.Remove(id);
                }
            }
        }
    }

    private static bool SameValue(GeneState first, GeneState second) =>
        first.Allele == second.Allele && first.Number == second.Number;
}
