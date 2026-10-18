namespace Content.Shared._RedStar.Genetics;

public abstract partial class GenomeSystem : EntitySystem
{
    /// <summary>
    /// Returns false when the entity has no genome or its initial generation has not completed.
    /// </summary>
    public bool IsGenerated(EntityUid uid) =>
        TryComp<GenomeComponent>(uid, out var genome) && genome.GenomeGenerated;
}
