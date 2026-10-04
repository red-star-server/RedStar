using Content.Shared.Movement.Systems;

namespace Content.Shared._DV;

public sealed partial class CarryingSlowdownSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;

    public void SetModifier(Entity<CarryingSlowdownComponent?> ent, float modifier)
    {
        ent.Comp ??= EnsureComp<CarryingSlowdownComponent>(ent);
        ent.Comp.Modifier = modifier;
        Dirty(ent, ent.Comp);

        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    [SubscribeLocalEvent]
    private void OnRefreshMoveSpeed(Entity<CarryingSlowdownComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.Modifier, ent.Comp.Modifier);
    }
}
