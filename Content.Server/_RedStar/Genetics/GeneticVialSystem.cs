using System.Linq;
using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Validates and changes the contents of a physical genetic vial.
/// </summary>
public sealed partial class GeneticVialSystem : EntitySystem
{
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

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
