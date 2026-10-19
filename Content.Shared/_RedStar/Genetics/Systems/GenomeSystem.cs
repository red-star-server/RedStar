using Content.Shared._RedStar.Genetics.Components;
namespace Content.Shared._RedStar.Genetics.Systems;

public abstract partial class GenomeSystem : EntitySystem
{
    /// <summary>
    /// Expresses reserved load relative to an organism's safe capacity. An empty report has no load.
    /// </summary>
    public static float CalculateLoadPercentage(int load, int capacity) => capacity > 0 ? 100f * load / capacity : 0f;

    /// <summary>
    /// Returns false when the entity has no genome or its initial generation has not completed.
    /// </summary>
    public bool IsGenerated(Entity<GenomeComponent?> ent) =>
        Resolve(ent, ref ent.Comp, logMissing: false) && ent.Comp.GenomeGenerated;
}
