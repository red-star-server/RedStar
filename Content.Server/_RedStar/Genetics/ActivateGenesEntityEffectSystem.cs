using Content.Shared._RedStar.EntityEffects.Effects;
using Content.Shared.EntityEffects;
using Content.Shared._RedStar.Genetics;

namespace Content.Server._RedStar.Genetics;

public sealed partial class ActivateGenesEntityEffectSystem : EntityEffectSystem<GenomeComponent, ActivateGenes>
{
    [Dependency] private ServerGenomeSystem _genomes = default!;

    protected override void Effect(Entity<GenomeComponent> entity, ref EntityEffectEvent<ActivateGenes> args) =>
        _genomes.ActivateGenes(entity.AsNullable());
}
