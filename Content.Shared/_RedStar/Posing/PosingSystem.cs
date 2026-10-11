using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage.Components;
using Content.Shared.Input;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Input.Binding;

namespace Content.Shared._RedStar.Posing;

public abstract partial class PosingSystem : EntitySystem
{
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.TogglePosing, InputCmdHandler.FromDelegate(session =>
            {
                if (session?.AttachedEntity is { } uid)
                    Toggle(uid);
            }, handle: false))
            .Bind(ContentKeyFunctions.PosingOffsetLeft, InputCmdHandler.FromDelegate(session => Adjust(session?.AttachedEntity, new(-0.05f, 0f)), handle: false))
            .Bind(ContentKeyFunctions.PosingOffsetRight, InputCmdHandler.FromDelegate(session => Adjust(session?.AttachedEntity, new(0.05f, 0f)), handle: false))
            .Bind(ContentKeyFunctions.PosingOffsetUp, InputCmdHandler.FromDelegate(session => Adjust(session?.AttachedEntity, new(0f, 0.05f)), handle: false))
            .Bind(ContentKeyFunctions.PosingOffsetDown, InputCmdHandler.FromDelegate(session => Adjust(session?.AttachedEntity, new(0f, -0.05f)), handle: false))
            .Bind(ContentKeyFunctions.PosingRotateNegative, InputCmdHandler.FromDelegate(session => Rotate(session?.AttachedEntity, 5f), handle: false))
            .Bind(ContentKeyFunctions.PosingRotatePositive, InputCmdHandler.FromDelegate(session => Rotate(session?.AttachedEntity, -5f), handle: false))
            .Register<PosingSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<PosingSystem>();
    }

    [SubscribeLocalEvent]
    private void OnUpdateCanMove(Entity<PosingComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.Active)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnDowned(Entity<PosingComponent> ent, ref DownedEvent args)
    {
        if (ent.Comp.Active)
            SetActive(ent, false);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<PosingComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.Active && args.NewMobState != MobState.Alive)
            SetActive(ent, false);
    }

    private void Toggle(EntityUid uid)
    {
        if (!TryComp<PosingComponent>(uid, out var component))
            return;

        if (!component.Active &&
            (!_actionBlocker.CanConsciouslyPerformAction(uid) ||
             TryComp<StaminaComponent>(uid, out var stamina) && stamina.Critical ||
             HasComp<StunnedComponent>(uid) || _standing.IsDown(uid)))
            return;

        SetActive((uid, component), !component.Active);
    }

    private void SetActive(Entity<PosingComponent> ent, bool active)
    {
        ent.Comp.Active = active;
        ent.Comp.Offset = Vector2.Zero;
        ent.Comp.Rotation = Angle.Zero;
        Dirty(ent);
        _actionBlocker.UpdateCanMove(ent.Owner);
        OnPosingChanged(ent.Owner, active);
    }

    private void Adjust(EntityUid? uid, Vector2 delta)
    {
        if (uid is not { } entity || !TryComp<PosingComponent>(entity, out var component) || !component.Active)
            return;

        var offset = Vector2.Clamp(component.Offset + delta, -component.OffsetLimit, component.OffsetLimit);
        if (offset == component.Offset)
            return;

        component.Offset = offset;
        Dirty(entity, component);
    }

    private void Rotate(EntityUid? uid, float degrees)
    {
        if (uid is not { } entity || !TryComp<PosingComponent>(entity, out var component) || !component.Active)
            return;

        var angle = Angle.FromDegrees(Math.Clamp(component.Rotation.Degrees + degrees, -component.RotationLimit, component.RotationLimit));
        if (angle == component.Rotation)
            return;

        component.Rotation = angle;
        Dirty(entity, component);
    }

    protected virtual void OnPosingChanged(EntityUid uid, bool active) { }
}
