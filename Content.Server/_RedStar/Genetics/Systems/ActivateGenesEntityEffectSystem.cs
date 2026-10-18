using Content.Shared.EntityEffects;
using Content.Shared._RedStar.EntityEffects.Effects;
using Content.Shared._RedStar.Genetics.Components;

namespace Content.Server._RedStar.Genetics.Systems;

public sealed partial class ActivateGenesEntityEffectSystem : EntityEffectSystem<GenomeComponent, ActivateGenes>
{
    [Dependency] private ServerGenomeSystem _genomes = default!;

    protected override void Effect(Entity<GenomeComponent> entity, ref EntityEffectEvent<ActivateGenes> args) =>
        _genomes.ActivateGenes(entity.AsNullable());
}
