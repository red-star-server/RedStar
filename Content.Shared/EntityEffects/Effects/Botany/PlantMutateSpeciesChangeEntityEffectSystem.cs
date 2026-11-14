using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Random.Helpers; // RS14: transition for existing RedStar prototypes.
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// Changes the planted plant's species by replacing the plant entity with a new entity spawned from one
/// of the current plant's directed mutation edges. // RS14
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateSpeciesChangeEntityEffectSystem : EntityEffectSystem<PlantDataComponent, PlantMutateSpeciesChange>
{
    [Dependency] private PlantMutationSystem _mutation = default!;
    [Dependency] private IGameTiming _timing = default!; // RS14

    protected override void Effect(Entity<PlantDataComponent> entity, ref EntityEffectEvent<PlantMutateSpeciesChange> args)
    {
        if (entity.Comp.Mutations.Count > 0)
        {
            _mutation.TrySpeciesChange(entity.Owner); // RS14
            return;
        }

        // RS14: preserve the existing mutation behavior until content is converted in the next PR.
        if (entity.Comp.MutationPrototypes.Count == 0)
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));
        _mutation.SpeciesChange(entity.Owner, random.Pick(entity.Comp.MutationPrototypes));
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantMutateSpeciesChange : EntityEffectBase<PlantMutateSpeciesChange>;
