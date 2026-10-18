using Content.Server._RedStar.Xenobiology;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

public sealed partial class GeneticVialSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SampleSystem _samples = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GeneticVialComponent> ent, ref MapInitEvent args) => UpdateVisual(ent);

    /// <summary>
    /// Returns whether the vial lacks the gene; does not validate the gene prototype or sample stability.
    /// </summary>
    public bool CanAdd(Entity<GeneticVialComponent> vial, ProtoId<GenePrototype> id) =>
        !vial.Comp.Genes.Contains(id);

    /// <summary>
    /// Moves a gene from the selected sample into the vial if stability permits and the vial lacks it.
    /// Observers are notified after the vial contains the gene; a rejected extraction leaves both unchanged.
    /// </summary>
    public bool TryExtract(Entity<SampleContainerComponent> dish, int sampleId,
        Entity<GeneticVialComponent> vial, ProtoId<GenePrototype> id)
    {
        if (dish.Comp.Samples.Find(candidate => candidate.Id == sampleId) is not { } sample ||
            !_prototypes.TryIndex(id, out var gene) || sample.Stability < gene.MinimumStability ||
            sample.GetGenome() is not { } genome || !genome.Genes.Contains(id) || !CanAdd(vial, id))
            return false;

        // Sample observers must see the extracted gene in the vial when rebuilding the UI.
        vial.Comp.Genes.Add(id);
        vial.Comp.Revision++;
        if (!_samples.TryRemoveGene(dish.AsNullable(), sampleId, id))
        {
            vial.Comp.Genes.Remove(id);
            vial.Comp.Revision++;
            return false;
        }
        UpdateVisual(vial);
        return true;
    }

    /// <summary>
    /// Removes all genes and updates the contents revision and appearance. An empty vial is unchanged.
    /// </summary>
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
