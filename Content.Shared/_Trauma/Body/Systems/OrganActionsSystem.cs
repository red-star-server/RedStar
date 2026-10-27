using Content.Shared._Trauma.Common.Body;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Body;

namespace Content.Shared._Trauma.Body;

public sealed partial class OrganActionsSystem : EntitySystem
{
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private EntityQuery<OrganComponent> _organQuery;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<OrganActionsComponent> ent, ref MapInitEvent args)
    {
        var actions = EnsureComp<ActionsContainerComponent>(ent);
        foreach (var id in ent.Comp.Actions)
        {
            _actionContainer.AddAction(ent, id, actions);
        }
    }

    [SubscribeLocalEvent]
    private void OnEnabled(Entity<OrganActionsComponent> ent, ref OrganEnabledEvent args)
    {
        var container = EnsureComp<ActionsContainerComponent>(ent);
        _actions.GrantContainedActions(args.Body, (ent, container));
    }

    [SubscribeLocalEvent]
    private void OnDisabled(Entity<OrganActionsComponent> ent, ref OrganDisabledEvent args)
    {
        _actions.RemoveProvidedActions(args.Body, ent.Owner);
    }
}
