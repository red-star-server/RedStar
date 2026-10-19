using Content.Shared._RedStar.Genetics.Components;
namespace Content.Shared._RedStar.Genetics.Systems;

public abstract partial class GenomeSystem : EntitySystem
{
    /// <summary>
    /// Returns false when the entity has no genome or its initial generation has not completed.
    /// </summary>
    public bool IsGenerated(Entity<GenomeComponent?> ent) =>
        Resolve(ent, ref ent.Comp, logMissing: false) && ent.Comp.GenomeGenerated;
}
