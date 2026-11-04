using Content.Shared.Item.ItemToggle;

namespace Content.Shared._Trauma.Augments;

public sealed partial class AugmentStrengthSystem : EntitySystem
{
    [Dependency] private ItemToggleSystem _toggle = default!;

    [SubscribeLocalEvent]
    private void OnGetMeleeDamage(Entity<AugmentStrengthComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        if (_toggle.IsActivated(ent.Owner))
            args.Damage *= ent.Comp.Modifier;
    }
}
