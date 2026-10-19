using System.Linq;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared._RedStar.Genetics.Systems;
using Content.Shared._RedStar.Genetics.UI;
using Content.Shared.Interaction.Events;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Genetics.Systems;

public sealed partial class ServerGeneticAnalyzerSystem : GeneticAnalyzerSystem
{
    [Dependency] private ServerGenomeSystem _genomes = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    protected override void CompleteScan(Entity<GeneticAnalyzerComponent> analyzer, EntityUid target, EntityUid user)
    {
        analyzer.Comp.Target = target;
        analyzer.Comp.NextUpdate = _timing.CurTime + analyzer.Comp.UpdateInterval;
        UpdateReport(analyzer);
        _ui.OpenUi(analyzer.Owner, GeneticAnalyzerUiKey.Key, user);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<GeneticAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var analyzer))
        {
            if (analyzer.Target is null || analyzer.NextUpdate > _timing.CurTime)
                continue;
            analyzer.NextUpdate = _timing.CurTime + analyzer.UpdateInterval;
            UpdateReport((uid, analyzer));
        }
    }

    private void UpdateReport(Entity<GeneticAnalyzerComponent> analyzer)
    {
        if (analyzer.Comp.Target is not { } target || TerminatingOrDeleted(target) ||
            !_transform.InRange(Transform(analyzer).Coordinates, Transform(target).Coordinates, analyzer.Comp.MaxScanRange) ||
            !_genomes.TryGetSnapshot(target, out var snapshot))
        {
            PauseReport(analyzer);
            return;
        }

        var genome = Comp<GenomeComponent>(target);
        var genes = snapshot.Genes.OrderBy(id => id.Id, StringComparer.Ordinal)
            .Select(id => new GeneticAnalyzerGene(id, _genomes.TryGetGeneStatus(target, id, out var status) ? status : null))
            .ToList();
        _ui.SetUiState(analyzer.Owner, GeneticAnalyzerUiKey.Key,
            new GeneticAnalyzerUiState(GetNetEntity(target), true, _genomes.GetLoad(target), genome.MaximumLoad, genes));
    }

    private void PauseReport(Entity<GeneticAnalyzerComponent> analyzer)
    {
        if (_ui.TryGetUiState<GeneticAnalyzerUiState>(analyzer.Owner, GeneticAnalyzerUiKey.Key, out var state) && state.InRange)
        {
            _ui.SetUiState(analyzer.Owner, GeneticAnalyzerUiKey.Key,
                new GeneticAnalyzerUiState(state.Target, false, state.Load, state.MaximumLoad, state.Genes));
        }
    }

    [SubscribeLocalEvent]
    private void OnDropped(Entity<GeneticAnalyzerComponent> ent, ref DroppedEvent args) => StopReport(ent);

    [SubscribeLocalEvent]
    private void OnStored(Entity<GeneticAnalyzerComponent> ent, ref EntGotInsertedIntoContainerMessage args) => StopReport(ent);

    private void StopReport(Entity<GeneticAnalyzerComponent> analyzer)
    {
        analyzer.Comp.Target = null;
        PauseReport(analyzer);
    }
}
