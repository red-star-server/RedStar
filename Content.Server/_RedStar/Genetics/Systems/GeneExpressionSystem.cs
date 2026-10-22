using System.Linq;
using Content.Server._RedStar.Genetics.Components;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared._RedStar.Genetics.Events;
using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics.Systems;

/// <summary>
/// Owns persistent gene manifestations. Each manifestation has its own status entity;
/// native status components contribute through the standard status-effect event relay.
/// </summary>
public sealed partial class GeneExpressionSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedContainerSystem _containers = default!;

    /// <summary>
    /// Creates a gene's manifestation, or retains its existing one. Does not replay activation effects.
    /// </summary>
    public bool TryStart(EntityUid target, GenePrototype gene)
    {
        if (gene.Expression is not { } prototype)
            return true;

        return _status.TrySetStatusEffectDuration(target, prototype);
    }

    /// <summary>
    /// Removes only this gene's manifestation. Other genes and independent status effects remain.
    /// </summary>
    public void Stop(EntityUid target, GenePrototype gene)
    {
        if (gene.Expression is { } prototype && _status.TryGetStatusEffect(target, prototype, out var effect))
            Del(effect.Value);
    }

    [SubscribeLocalEvent]
    private void OnApplied(Entity<GeneExpressionComponent> ent, ref StatusEffectAppliedEvent args) =>
        Acquire(ent, args.Target);

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GeneExpressionComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<StatusEffectComponent>(ent, out var status) && status.AppliedTo is { } target && status.Applied)
            Acquire(ent, target);
    }

    private void Acquire(Entity<GeneExpressionComponent> ent, EntityUid target)
    {
        var state = EnsureComp<GeneContributionsComponent>(target);
        foreach (var (name, prototype) in ent.Comp.Solutions)
        {
            if (!state.Solutions.TryGetValue(name, out var contribution))
            {
                contribution = new GeneSolutionContribution();
                state.Solutions.Add(name, contribution);
            }
            contribution.Sources.Add(ent.Comp.Gene);
            if (_solutions.TryGetSolution(target, name, out _))
                continue;

            var manager = EnsureComp<SolutionManagerComponent>(target);
            var container = _containers.EnsureContainer<Container>(target, manager.Container);
            contribution.Owned = _solutions.CreateSolution(prototype, container).Owner;
        }
        foreach (var (name, definition) in ent.Comp.Components)
        {
            var type = Factory.GetRegistration(name).Type;
            if (!state.Components.TryGetValue(name, out var contribution))
            {
                var exists = TryComp(target, type, out var existing);
                contribution = new GeneComponentContribution { Owned = !exists, Instance = existing };
                state.Components.Add(name, contribution);
            }
            contribution.Sources.Add(ent.Comp.Gene);

            if (!TryComp(target, type, out var component))
            {
                var registry = new ComponentRegistry { [name] = definition };
                EntityManager.AddComponents(target, registry, removeExisting: false);
                component = EntityManager.GetComponent(target, type);
                contribution.Owned = true;
                contribution.Instance = component;
            }
            contribution.Instance ??= component;
        }
        foreach (var prototype in ent.Comp.Actions)
        {
            if (!state.Actions.TryGetValue(prototype, out var contribution))
            {
                contribution = new GeneActionContribution();
                state.Actions.Add(prototype, contribution);
                var exists = TryComp<ActionsComponent>(target, out var actions) && actions.Actions.Any(action =>
                    MetaData(action).EntityPrototype?.ID == prototype.Id);
                if (!exists)
                    contribution.Owned = _actions.AddAction(target, prototype.Id);
            }
            contribution.Sources.Add(ent.Comp.Gene);
        }
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<GeneExpressionComponent> ent, ref StatusEffectRemovedEvent args)
    {
        var target = args.Target;
        if (TerminatingOrDeleted(target))
            return;

        var state = EnsureComp<GeneContributionsComponent>(target);

        foreach (var prototype in ent.Comp.Actions)
        {
            if (!state.Actions.TryGetValue(prototype, out var contribution))
                continue;
            contribution.Sources.Remove(ent.Comp.Gene);
            if (contribution.Sources.Count != 0)
                continue;
            state.Actions.Remove(prototype);
            if (contribution.Owned is not { } owned || TerminatingOrDeleted(owned))
                continue;
            _actions.RemoveAction(target, owned);
            Del(owned);
        }

        foreach (var name in ent.Comp.Components.Keys.Reverse())
        {
            if (!state.Components.TryGetValue(name, out var contribution))
                continue;
            contribution.Sources.Remove(ent.Comp.Gene);
            if (contribution.Sources.Count != 0)
                continue;

            state.Components.Remove(name);
            var type = Factory.GetRegistration(name).Type;
            if (contribution.Owned && TryComp(target, type, out var current) &&
                (contribution.Instance is null || ReferenceEquals(contribution.Instance, current)))
                RemComp(target, current);
        }

        foreach (var name in ent.Comp.Solutions.Keys)
        {
            if (!state.Solutions.TryGetValue(name, out var contribution))
                continue;
            contribution.Sources.Remove(ent.Comp.Gene);
            if (contribution.Sources.Count != 0)
                continue;

            state.Solutions.Remove(name);
            if (contribution.Owned is { } owned && _solutions.TryGetSolution(target, name, out var current) &&
                current.Value.Owner == owned)
                Del(owned);
        }
        var ended = new GeneExpressionEndedEvent(ent.Comp.Gene);
        RaiseLocalEvent(target, ref ended);
    }
}
