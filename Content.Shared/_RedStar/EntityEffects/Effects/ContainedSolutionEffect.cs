using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.EntityEffects.Effects;

/// <summary>
/// Adds a named solution entity without replacing the target's other solutions.
/// </summary>
public sealed partial class ContainedSolutionEffectSystem : EntityEffectSystem<MetaDataComponent, ContainedSolutionEffect>
{
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedContainerSystem _containers = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ContainedSolutionEffect> args)
    {
        var effect = args.Effect;
        if (_solutions.TryGetSolution(entity.Owner, effect.SolutionName, out _))
            return;

        var manager = EnsureComp<SolutionManagerComponent>(entity);
        var container = _containers.EnsureContainer<Container>(entity, manager.Container);
        _solutions.CreateSolution(effect.Prototype, container);
    }
}

public sealed partial class ContainedSolutionEffect : EntityEffectBase<ContainedSolutionEffect>
{
    [DataField(required: true)] public EntProtoId Prototype;
    [DataField(required: true)] public string SolutionName = string.Empty;
}
