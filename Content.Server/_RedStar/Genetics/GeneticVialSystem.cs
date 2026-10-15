using Content.Server._RedStar.Xenobiology;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology;
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

    public bool CanAdd(Entity<GeneticVialComponent> vial, ProtoId<GenePrototype> id) =>
        !vial.Comp.Genes.Contains(id);

    public bool TryExtract(Entity<SampleContainerComponent> dish, BiologicalSample sample,
        Entity<GeneticVialComponent> vial, ProtoId<GenePrototype> id)
    {
        if (!dish.Comp.Samples.Contains(sample) || sample.Stability < _prototypes.Index(id).MinimumStability ||
            sample.GetGenome() is not { } genome || !genome.Genes.Contains(id) || !CanAdd(vial, id))
            return false;

        // Sample observers must see the extracted gene in the vial when rebuilding the UI.
        vial.Comp.Genes.Add(id);
        vial.Comp.Revision++;
        _samples.TryRemoveGene((dish.Owner, (SampleContainerComponent?) dish.Comp), sample, id);
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
