using System.Linq;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Validates and changes the contents of a physical genetic vial.
/// </summary>
public sealed partial class GeneticVialSystem : EntitySystem
{
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedCellSystem _cells = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GeneticVialComponent> ent, ref MapInitEvent args) => UpdateVisual(ent);

    public Dictionary<ProtoId<GenePrototype>, GeneState> GetGenes(Entity<GeneticVialComponent> vial) =>
        vial.Comp.Genes.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());

    public bool CanAdd(Entity<GeneticVialComponent> vial, ProtoId<GenePrototype> id, GeneState state) =>
        !vial.Comp.Genes.ContainsKey(id) &&
        _genomes.ValidateGene(id, state) == GenomeOperationResult.Success;

    public bool TryAdd(Entity<GeneticVialComponent> vial, ProtoId<GenePrototype> id, GeneState state)
    {
        if (!CanAdd(vial, id, state))
            return false;

        vial.Comp.Genes.Add(id, state.Clone());
        vial.Comp.Revision++;
        UpdateVisual(vial);
        return true;
    }

    /// <summary>
    /// Transfers a validated gene from a contained, stable sample into a vial.
    /// Restores the vial if removing the sample gene unexpectedly fails.
    /// </summary>
    public bool TryExtract(Entity<CellContainerComponent> dish, Cell cell,
        Entity<GeneticVialComponent> vial, ProtoId<GenePrototype> id, GeneState state)
    {
        if (!dish.Comp.Cells.Contains(cell) || !float.IsFinite(cell.Stability) || cell.Stability < 0.5f ||
            cell.GetGenome() is not { } genome ||
            _genomes.ValidateSnapshot(genome) != GenomeOperationResult.Success ||
            !genome.TryGetGene(id, out var stored) || stored is null ||
            stored.Allele != state.Allele || stored.Number != state.Number || !CanAdd(vial, id, state))
            return false;

        // All checks precede mutation. Both calls run synchronously on the server.
        if (!TryAdd(vial, id, state))
            return false;
        if (_cells.TryRemoveGene((dish.Owner, (CellContainerComponent?) dish.Comp), cell, id))
            return true;

        TryRemove(vial, id);
        return false;
    }

    public bool TryRemove(Entity<GeneticVialComponent> vial, ProtoId<GenePrototype> id)
    {
        if (!vial.Comp.Genes.Remove(id))
            return false;

        vial.Comp.Revision++;
        UpdateVisual(vial);
        return true;
    }

    public void Clear(Entity<GeneticVialComponent> vial)
    {
        if (vial.Comp.Genes.Count == 0)
            return;

        vial.Comp.Genes.Clear();
        vial.Comp.Revision++;
        UpdateVisual(vial);
    }

    private void UpdateVisual(Entity<GeneticVialComponent> vial) =>
        _appearance.SetData(vial, GeneticVialVisuals.Filled, vial.Comp.Genes.Count > 0);
}
