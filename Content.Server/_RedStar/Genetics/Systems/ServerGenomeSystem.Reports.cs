using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared._RedStar.Genetics.UI;

namespace Content.Server._RedStar.Genetics.Systems;

public sealed partial class ServerGenomeSystem
{
    /// <summary>
    /// Builds a live report containing natural and acquired genes, expression states and current load.
    /// Returns false until the organism's genome has been initialized.
    /// </summary>
    public bool TryGetReport(Entity<GenomeComponent?> ent, [NotNullWhen(true)] out GeneticAnalyzerUiState? report)
    {
        report = null;
        if (!Resolve(ent, ref ent.Comp, logMissing: false) || !TryGetSnapshot(ent, out var snapshot))
            return false;

        var genes = snapshot.Genes.OrderBy(id => id.Id, StringComparer.Ordinal)
            .Select(id => new GeneticAnalyzerGene(id, ent.Comp.Genes.TryGetValue(id, out var status) ? status : null))
            .ToList();
        report = new GeneticAnalyzerUiState(GetNetEntity(ent.Owner), true, GetLoad(ent), ent.Comp.MaximumLoad, genes);
        return true;
    }
}
