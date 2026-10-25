using Content.Shared._Trauma.Common.Body;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell;

namespace Content.Shared._Trauma.Augments;

public sealed partial class AugmentPowerDrawSystem : EntitySystem
{
    [Dependency] private AugmentSystem _augment = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private AugmentPowerCellSystem _augmentPower = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;

    [SubscribeLocalEvent]
    private void OnGetPowerDraw(Entity<AugmentPowerDrawComponent> ent, ref GetAugmentsPowerDrawEvent args)
    {
        if (_toggle.IsActivated(ent.Owner))
            args.TotalDraw += ent.Comp.Draw;
    }

    [SubscribeLocalEvent]
    private void OnOrganDisabled(Entity<AugmentPowerDrawComponent> ent, ref OrganDisabledEvent args)
    {
        _toggle.TryDeactivate(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnActivateAttempt(Entity<AugmentPowerDrawComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (_augment.GetBody(ent) is not {} body ||
            _augmentPower.GetBodyAugment(body) is not {} slot ||
            !_powerCell.HasActivatableCharge(slot.Owner))
        {
            args.Cancelled = true;
        }
    }

    [SubscribeLocalEvent]
    private void OnToggled(Entity<AugmentPowerDrawComponent> ent, ref ItemToggledEvent args)
    {
        if (_augment.GetBody(ent) is {} body && _augmentPower.GetBodyAugment(body) is {} slot)
            _augmentPower.UpdateDrawRate(slot.Owner);
    }
}
