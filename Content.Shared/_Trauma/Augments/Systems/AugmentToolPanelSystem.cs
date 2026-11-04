using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._Trauma.Augments;

public sealed partial class AugmentToolPanelSystem : EntitySystem
{
    [Dependency] private AugmentPowerCellSystem _augmentPowerCell = default!;
    [Dependency] private AugmentSystem _augment = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private EntityQuery<BodyPartComponent> _partQuery;
    [Dependency] private EntityQuery<ChildOrganComponent> _childQuery;
    [Dependency] private EntityQuery<HandsComponent> _handsQuery;

    [SubscribeLocalEvent]
    private void OnDropAttempt(Entity<AugmentToolPanelActiveItemComponent> ent, ref ContainerGettingRemovedAttemptEvent args)
    {
        if (_timing.ApplyingState)
            return;

        // you can never drop an active tool panel item, it has to be retracted with the action
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnOrganDisabled(Entity<AugmentToolPanelComponent> augment, ref OrganDisabledEvent args)
    {
        // items automatically retract if you lose power / get your arm cut off
        SwitchTool(augment, null, args.Body);
    }

    [SubscribeLocalEvent]
    private void OnSwitchTool(Entity<AugmentToolPanelComponent> augment, ref AugmentToolPanelSwitchMessage args)
    {
        if (_augment.GetBody(augment) is not {} body || !TryGetHand(augment, body, out var hand))
            return;

        var desiredTool = GetEntity(args.DesiredTool);
        if (desiredTool == null && _hands.GetHeldItem(body, hand) == null)
            return;

        if (_hands.GetHeldItem(body, hand) is {} held && !HasComp<AugmentToolPanelActiveItemComponent>(held))
        {
            _popup.PopupEntity(Loc.GetString("augment-tool-panel-hand-full"), body, body, PopupType.SmallCaution);
            return;
        }

        if (desiredTool is {} tool &&
            (!_container.TryGetContainingContainer(tool, out var container) || container.Owner != augment.Owner))
            return;

        if (!_augmentPowerCell.TryUseChargeBody(body, augment.Comp.SwitchCharge))
            return;

        SwitchTool(augment, desiredTool, body, hand);
    }

    /// <summary>
    /// Switches to a tool using a hand derived from the augment's arm.
    /// </summary>
    public void SwitchTool(Entity<AugmentToolPanelComponent> augment, EntityUid? tool, EntityUid body)
    {
        if (TryGetHand(augment, body, out var hand))
            SwitchTool(augment, tool, body, hand);
    }

    private bool TryGetHand(Entity<AugmentToolPanelComponent> augment, EntityUid body, out string hand)
    {
        hand = string.Empty;
        if (!_handsQuery.TryComp(body, out var handsComp) ||
            !_childQuery.TryComp(augment, out var child) ||
            child.Parent is not {} partUid || TerminatingOrDeleted(partUid))
            return false;

        // The arm's symmetry is the same as the hand.
        var location = _partQuery.Comp(partUid).Symmetry switch
        {
            BodyPartSymmetry.Left => HandLocation.Left,
            BodyPartSymmetry.Right => HandLocation.Right,
            _ => HandLocation.Middle,
        };

        foreach (var (name, data) in handsComp.Hands)
        {
            if (data.Location != location)
                continue;

            hand = name;
            return true;
        }

        _popup.PopupEntity(Loc.GetString("augment-tool-panel-no-hand"), body, body, PopupType.LargeCaution);
        return false;
    }

    /// <summary>
    /// Switches to a tool using the specified hand.
    /// </summary>
    public void SwitchTool(Entity<AugmentToolPanelComponent> augment, EntityUid? desiredTool, EntityUid body, string hand)
    {
        if (_hands.GetHeldItem(body, hand) is {} item)
        {
            // if we have a tool that's currently out, deposit it back into the storage
            if (RemComp<AugmentToolPanelActiveItemComponent>(item))
            {
                if (!_storage.PlayerInsertEntityInWorld(augment.Owner, body, item))
                {
                    Log.Error($"Inserting tool {ToPrettyString(item)} back into {ToPrettyString(augment)} failed");
                    // prevent exploits
                    EnsureComp<AugmentToolPanelActiveItemComponent>(item);
                    return;
                }

                if (desiredTool == null) // don't double popup, only show it when deselecting
                    _popup.PopupEntity(Loc.GetString("augment-tool-panel-retracted", ("item", item)), body, body);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("augment-tool-panel-hand-full"), body, body, PopupType.SmallCaution);
                return;
            }

            // no longer holding a tool, stop drawing power
            _toggle.TryDeactivate(augment.Owner, user: body);
        }

        if (desiredTool is not {} tool)
            return;

        // evil malf client trying to pick up arbitrary entities!
        if (!_container.TryGetContainingContainer(tool, out var container) || container.Owner != augment.Owner)
        {
            Log.Warning($"{ToPrettyString(body)} tried to pick up {ToPrettyString(tool)} which was not inside {ToPrettyString(augment)}!");
            return;
        }

        if (!_toggle.TryActivate(augment.Owner, user: body))
            return;

        if (!_hands.TryPickup(body, tool, hand))
        {
            _toggle.TryDeactivate(augment.Owner, user: body);
            _popup.PopupEntity(Loc.GetString("augment-tool-panel-cannot-pick-up"), body, body, PopupType.SmallCaution);
            return;
        }

        EnsureComp<AugmentToolPanelActiveItemComponent>(tool);
        _popup.PopupEntity(Loc.GetString("augment-tool-panel-selected", ("item", tool)), body, body);
    }
}
