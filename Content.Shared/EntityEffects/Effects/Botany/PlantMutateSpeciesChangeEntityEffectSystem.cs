using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// Changes the planted plant's species by replacing the plant entity with a new entity spawned from one
/// of the current plant's directed mutation edges. // RS14
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateSpeciesChangeEntityEffectSystem : EntityEffectSystem<PlantDataComponent, PlantMutateSpeciesChange>
{
    [Dependency] private PlantMutationSystem _mutation = default!;

    protected override void Effect(Entity<PlantDataComponent> entity, ref EntityEffectEvent<PlantMutateSpeciesChange> args)
    {
        _mutation.TrySpeciesChange(entity.Owner); // RS14
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantMutateSpeciesChange : EntityEffectBase<PlantMutateSpeciesChange>;
