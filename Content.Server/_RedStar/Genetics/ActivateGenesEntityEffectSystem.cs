using Content.Shared._RedStar.EntityEffects.Effects;
using Content.Shared.EntityEffects;

namespace Content.Server._RedStar.Genetics;

public sealed partial class ActivateGenesEntityEffectSystem : EntityEffectSystem<GenomeComponent, ActivateGenes>
{
    [Dependency] private GenomeSystem _genomes = default!;

    protected override void Effect(Entity<GenomeComponent> entity, ref EntityEffectEvent<ActivateGenes> args) =>
        _genomes.ActivateGenes(entity.Owner);
}
