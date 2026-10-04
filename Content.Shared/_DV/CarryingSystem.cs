using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Resist;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;

namespace Content.Shared._DV;

public sealed partial class CarryingSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private CarryingSlowdownSystem _slowdown = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StandingStateSystem _standingState = default!;
    [Dependency] private SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery;

    [SubscribeLocalEvent]
    private void AddCarryVerb(Entity<CarriableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        var target = args.Target;

        if (!args.CanInteract || !args.CanAccess || user == target)
            return;

        if (!CanCarry(user, ent))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => StartCarryDoAfter(user, ent),
            Text = Loc.GetString("carry-verb"),
            Priority = 2,
        });
    }

    /// <summary>
    /// The carried entity occupies the carrier's hands through virtual items.
    /// If one of those virtual items disappears, drop the carried entity.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnVirtualItemDeleted(Entity<CarryingComponent> ent, ref VirtualItemDeletedEvent args)
    {
        if (args.BlockingEntity != ent.Comp.Carried)
            return;

        DropCarried(ent, ent.Comp.Carried);
    }

    /// <summary>
    /// Redirect throwing a carrying virtual item to throwing the actual carried entity.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnThrow(Entity<CarryingComponent> ent, ref BeforeThrowEvent args)
    {
        if (ent.Owner != args.PlayerUid)
            return;

        var carried = ent.Comp.Carried;
        if (!TryComp<VirtualItemComponent>(args.ItemUid, out var virtualItem) ||
            virtualItem.BlockingEntity != carried)
        {
            return;
        }

        args.ItemUid = carried;
        args.ThrowSpeed = Comp<CarriableComponent>(carried).ThrowSpeed * MassContest(ent, carried);
    }

    [SubscribeLocalEvent]
    private void OnParentChanged(Entity<CarryingComponent> ent, ref EntParentChangedMessage args)
    {
        var xform = Transform(ent);

        if (xform.MapUid != args.OldMapId)
            return;

        // Moving normally between map/grid parents is fine. Entering a container/entity is not.
        if (xform.ParentUid == xform.GridUid)
            return;

        DropCarried(ent, ent.Comp.Carried);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<CarryingComponent> ent, ref MobStateChangedEvent args)
    {
        DropCarried(ent, ent.Comp.Carried);
    }

    [SubscribeLocalEvent]
    private void OnDowned(Entity<CarryingComponent> ent, ref DownedEvent args)
    {
        DropCarried(ent, ent.Comp.Carried);
    }

    [SubscribeLocalEvent]
    private void OnMoveAttempt(Entity<BeingCarriedComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (!ent.Comp.Releasing)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnStandAttempt(Entity<BeingCarriedComponent> ent, ref StandAttemptEvent args)
    {
        if (!ent.Comp.Releasing)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnPullAttempt(Entity<BeingCarriedComponent> ent, ref PullAttemptEvent args)
    {
        if (!ent.Comp.Releasing)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnStartClimb(Entity<BeingCarriedComponent> ent, ref StartClimbEvent args)
    {
        DropCarried(ent.Comp.Carrier, ent);
    }

    [SubscribeLocalEvent]
    private void OnUnbuckled(Entity<BeingCarriedComponent> ent, ref UnbuckledEvent args)
    {
        DropCarried(ent.Comp.Carrier, ent);
    }

    [SubscribeLocalEvent]
    private void OnStrapped(Entity<BeingCarriedComponent> ent, ref StrappedEvent args)
    {
        DropCarried(ent.Comp.Carrier, ent);
    }


    [SubscribeLocalEvent]
    private void OnUnstrapped(Entity<BeingCarriedComponent> ent, ref UnstrappedEvent args)
    {
        DropCarried(ent.Comp.Carrier, ent);
    }

    [SubscribeLocalEvent]
    private void OnEscapeDrop(Entity<BeingCarriedComponent> ent, ref EscapeInventoryEvent args)
    {
        if (args.Cancelled)
            return;

        DropCarried(ent.Comp.Carrier, ent);
    }

    [SubscribeLocalEvent]
    private void OnBuckle(Entity<BeingCarriedComponent> ent, ref BuckledEvent args)
    {
        // Buckling already reparents the entity to the strapped object.
        DropCarried(ent.Comp.Carrier, ent, attachToGrid: false);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<BeingCarriedComponent> ent, ref ComponentShutdown args)
    {
        ReleaseCarried(ent);
    }

    [SubscribeLocalEvent]
    private void OnCarrierShutdown(Entity<CarryingComponent> ent, ref ComponentShutdown args)
    {
        DropCarried(ent, ent.Comp.Carried);
    }

    [SubscribeLocalEvent]
    private void OnCarrierTerminating(Entity<CarryingComponent> ent, ref EntityTerminatingEvent args)
    {
        // Detach before the engine recursively marks transform children for deletion.
        DropCarried(ent, ent.Comp.Carried);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<CarriableComponent> ent, ref CarryDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!CanCarry(args.Args.User, ent))
            return;

        Carry(args.Args.User, ent);
        args.Handled = true;
    }

    public void StartCarryDoAfter(EntityUid carrier, Entity<CarriableComponent> carried)
    {
        var length = GetPickupDuration(carrier, carried);

        if (length >= carried.Comp.MaximumPickupDuration)
        {
            _popup.PopupEntity(Loc.GetString("carry-too-heavy"), carried, carrier, PopupType.SmallCaution);
            return;
        }

        if (!HasComp<KnockedDownComponent>(carried))
            length *= carried.Comp.StandingPickupMultiplier;

        var ev = new CarryDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, carrier, length, ev, carried, target: carried)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        _popup.PopupEntity(Loc.GetString("carry-started", ("carrier", carrier)), carried, carried);
    }

    private void Carry(EntityUid carrier, Entity<CarriableComponent> carried)
    {
        if (TryComp<PullableComponent>(carried, out var carriedPullable))
            _pulling.TryStopPull(carried, carriedPullable);

        if (TryComp<PullableComponent>(carrier, out var carrierPullable))
            _pulling.TryStopPull(carrier, carrierPullable);

        var carrierXform = Transform(carrier);
        var carriedXform = Transform(carried);

        _transform.AttachToGridOrMap(carrier, carrierXform);
        _transform.AttachToGridOrMap(carried, carriedXform);
        _transform.SetParent(carried, carriedXform, carrier, carrierXform);

        var carrying = EnsureComp<CarryingComponent>(carrier);
        carrying.Carried = carried;
        Dirty(carrier, carrying);

        var beingCarried = EnsureComp<BeingCarriedComponent>(carried);
        beingCarried.Carrier = carrier;
        beingCarried.WasStanding = !_standingState.IsDown(carried.Owner);
        Dirty(carried, beingCarried);

        _standingState.Down(carried, playSound: false, dropHeldItems: false, force: true);
        ApplyCarrySlowdown(carrier, carried);
        _actionBlocker.UpdateCanMove(carried);

        if (_net.IsClient)
            return;

        var freeHandsRequired = GetRequiredHands(carrier, carried);

        for (var i = 0; i < freeHandsRequired; i++)
        {
            if (_virtualItem.TrySpawnVirtualItemInHand(carried, carrier))
                continue;

            DropCarried(carrier, carried);
            return;
        }
    }

    public void DropCarried(EntityUid carrier, EntityUid carried, bool attachToGrid = true)
    {
        if (!TryComp<BeingCarriedComponent>(carried, out var component) || component.Carrier != carrier || component.Releasing)
            return;

        ReleaseCarried((carried, component), attachToGrid);
        RemComp<BeingCarriedComponent>(carried);
    }

    private void CleanupCarrier(EntityUid carrier, EntityUid carried)
    {
        if (TerminatingOrDeleted(carrier) ||
            !TryComp<CarryingComponent>(carrier, out var carrying) || carrying.Carried != carried)
            return;

        // Remove the subscription before deleting virtual items, which raise deletion events.
        if (carrying.LifeStage < ComponentLifeStage.Stopping)
            RemComp<CarryingComponent>(carrier);

        RemComp<CarryingSlowdownComponent>(carrier);
        _virtualItem.DeleteInHandsMatching(carrier, carried);
        _movementSpeed.RefreshMovementSpeedModifiers(carrier);
    }

    private void ReleaseCarried(Entity<BeingCarriedComponent> ent, bool attachToGrid = true)
    {
        if (ent.Comp.Releasing)
            return;

        ent.Comp.Releasing = true;
        CleanupCarrier(ent.Comp.Carrier, ent);

        if (TerminatingOrDeleted(ent))
            return;

        _actionBlocker.UpdateCanMove(ent);
        if (attachToGrid)
            _transform.AttachToGridOrMap(ent);

        if (ent.Comp.WasStanding && !HasComp<KnockedDownComponent>(ent))
            _standingState.Stand(ent);
    }

    private void ApplyCarrySlowdown(EntityUid carrier, Entity<CarriableComponent> carried)
    {
        var massRatio = MassContest(carrier, carried);

        if (massRatio == 0f)
            massRatio = 1f;

        var carriable = carried.Comp;
        var massRatioSquared = massRatio * massRatio;
        var modifier = 1f - carriable.SpeedPenalty / massRatioSquared;
        modifier = Math.Max(carriable.MinimumSpeedModifier, modifier);

        _slowdown.SetModifier(carrier, modifier);
    }

    public bool CanCarry(EntityUid carrier, Entity<CarriableComponent> carried)
    {
        var handsRequired = GetRequiredHands(carrier, carried);

        return carrier != carried.Owner &&
               !HasComp<CarryingComponent>(carrier) &&
               HasComp<MapGridComponent>(Transform(carrier).ParentUid) &&
               !HasComp<BeingCarriedComponent>(carrier) &&
               !HasComp<BeingCarriedComponent>(carried) &&
               TryComp<HandsComponent>(carrier, out var hands) &&
               _hands.CountFreeHands((carrier, hands)) >= handsRequired;
    }

    private int GetRequiredHands(EntityUid carrier, Entity<CarriableComponent> carried)
    {
        return HasComp<CarrierOneHandComponent>(carrier) ? 1 : carried.Comp.FreeHandsRequired;
    }

    private float MassContest(EntityUid carrier, EntityUid target)
    {
        if (!_physicsQuery.TryComp(carrier, out var carrierPhysics) ||
            !_physicsQuery.TryComp(target, out var targetPhysics))
        {
            return 1f;
        }

        if (targetPhysics.FixturesMass == 0f)
            return 1f;

        return carrierPhysics.FixturesMass / targetPhysics.FixturesMass;
    }

    private TimeSpan GetPickupDuration(EntityUid carrier, Entity<CarriableComponent> carried)
    {
        var length = carried.Comp.PickupDuration;
        var modifier = MassContest(carrier, carried);

        if (modifier != 0f)
            length /= modifier;

        return length;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BeingCarriedComponent, TransformComponent>();
        while (query.MoveNext(out var carried, out var component, out var xform))
        {
            var carrier = component.Carrier;

            if (TerminatingOrDeleted(carrier))
            {
                DropCarried(carrier, carried);
                continue;
            }

            // Some systems can reparent entities without the expected event path.
            // Drop instead of leaving stale carrying state behind.
            if (xform.ParentUid != carrier)
            {
                DropCarried(carrier, carried);
                continue;
            }

            _transform.SetLocalPosition(carried, Vector2.Zero);
        }
    }
}
