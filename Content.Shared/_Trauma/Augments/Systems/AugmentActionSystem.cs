using Content.Shared._Trauma.Common.Body;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;

namespace Content.Shared._Trauma.Augments;

public sealed partial class AugmentActionSystem : EntitySystem
{
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private SharedActionsSystem _actions = default!;

    [SubscribeLocalEvent]
    private void OnOrganEnabled(Entity<AugmentActionComponent> ent, ref OrganEnabledEvent args)
    {
        EnsureComp<ActionsContainerComponent>(ent);
        _actions.AddAction(args.Body, ref ent.Comp.ActionEntity, ent.Comp.Action, ent);
    }

    [SubscribeLocalEvent]
    private void OnOrganDisabled(Entity<AugmentActionComponent> ent, ref OrganDisabledEvent args)
    {
        _actions.SetToggled(ent.Comp.ActionEntity, false);
        _actions.RemoveAction(args.Body, ent.Comp.ActionEntity);
    }

    [SubscribeLocalEvent]
    private void OnToggled(Entity<AugmentActionComponent> ent, ref ItemToggledEvent args)
    {
        _actions.SetToggled(ent.Comp.ActionEntity, args.Activated);
    }

    [SubscribeLocalEvent]
    private void OnToggleAction(Entity<AugmentActionComponent> ent, ref ToggleActionEvent args)
    {
        _toggle.Toggle(ent.Owner, user: args.Performer);
        args.Handled = true;
    }
}
