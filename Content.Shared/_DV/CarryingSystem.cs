using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
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

/// <summary>
/// Carrying implementation based on the Delta-V/Nyanotrasen carrying system.
/// PseudoItem/storage integration and fork-specific polymorph integration are intentionally omitted.
/// </summary>
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

    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        SubscribeLocalEvent<BeingCarriedComponent, StartClimbEvent>(OnDrop);
        SubscribeLocalEvent<BeingCarriedComponent, UnbuckledEvent>(OnDrop);
        SubscribeLocalEvent<BeingCarriedComponent, StrappedEvent>(OnDrop);
        SubscribeLocalEvent<BeingCarriedComponent, UnstrappedEvent>(OnDrop);
        SubscribeLocalEvent<BeingCarriedComponent, EscapeInventoryEvent>(OnDrop);
    }

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
        if (args.BlockingEntity == ent.Comp.Carried && HasComp<CarriableComponent>(args.BlockingEntity))
            DropCarried(ent, args.BlockingEntity);
    }

    /// <summary>
    /// Redirect throwing a carrying virtual item to throwing the actual carried entity.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnThrow(Entity<CarryingComponent> ent, ref BeforeThrowEvent args)
    {
        if (!TryComp<VirtualItemComponent>(args.ItemUid, out var virtualItem) ||
            virtualItem.BlockingEntity != ent.Comp.Carried)
        {
            return;
        }

        var carried = virtualItem.BlockingEntity;
        args.ItemUid = carried;
        args.ThrowSpeed = 5f * MassContest(ent, carried);
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

    // Kept intentionally permissive, matching the newer Floof/Delta-V behavior.
    // Blocking all interactions here prevents resistance and some legitimate self-interactions.
    [SubscribeLocalEvent]
    private void OnInteractionAttempt(Entity<BeingCarriedComponent> ent, ref InteractionAttemptEvent args)
    {
    }

    [SubscribeLocalEvent]
    private void OnMoveAttempt(Entity<BeingCarriedComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnStandAttempt(Entity<BeingCarriedComponent> ent, ref StandAttemptEvent args)
    {
        args.Cancel();
    }

    // Kept intentionally permissive, matching the newer Floof/Delta-V behavior.
    [SubscribeLocalEvent]
    private void OnInteractedWith(Entity<BeingCarriedComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
    }

    [SubscribeLocalEvent]
    private void OnPullAttempt(Entity<BeingCarriedComponent> ent, ref PullAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnDrop<TEvent>(Entity<BeingCarriedComponent> ent, ref TEvent args)
    {
        DropCarried(ent.Comp.Carrier, ent);
    }

    [SubscribeLocalEvent]
    private void OnBuckle(Entity<BeingCarriedComponent> ent, ref BuckledEvent args)
    {
        // Buckling already reparents the entity to the strapped object.
        DropCarried(ent.Comp.Carrier, ent, attachToGrid: false);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<BeingCarriedComponent> ent, ref ComponentRemove args)
    {
        if (!HasComp<CarryingComponent>(ent.Comp.Carrier))
            return;

        CleanupCarrier(ent.Comp.Carrier, ent);
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

        if (length.TotalSeconds >= 9f)
        {
            _popup.PopupClient(Loc.GetString("carry-too-heavy"), carried, carrier, PopupType.SmallCaution);
            return;
        }

        if (!HasComp<KnockedDownComponent>(carried))
            length *= 2f;

        var ev = new CarryDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, carrier, length, ev, carried, target: carried)
        {
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(args);
        _popup.PopupEntity(Loc.GetString("carry-started", ("carrier", carrier)), carried, carried);
    }

    private void Carry(EntityUid carrier, EntityUid carried)
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
        Dirty(carried, beingCarried);

        EnsureComp<KnockedDownComponent>(carried);
        ApplyCarrySlowdown(carrier, carried);
        _actionBlocker.UpdateCanMove(carried);

        if (_net.IsClient)
            return;

        var freeHandsRequired = Comp<CarriableComponent>(carried).FreeHandsRequired;
        if (HasComp<CarrierOneHandComponent>(carrier))
            freeHandsRequired = 1;

        for (var i = 0; i < freeHandsRequired; i++)
        {
            _virtualItem.TrySpawnVirtualItemInHand(carried, carrier);
        }
    }

    public bool TryCarry(EntityUid carrier, Entity<CarriableComponent?> toCarry)
    {
        if (!Resolve(toCarry, ref toCarry.Comp, false))
            return false;

        if (!CanCarry(carrier, (toCarry, toCarry.Comp)))
            return false;

        if (HasComp<BeingCarriedComponent>(carrier))
            return false;

        if (GetPickupDuration(carrier, toCarry).TotalSeconds > 9f)
            return false;

        Carry(carrier, toCarry);
        return true;
    }

    public void DropCarried(EntityUid carrier, EntityUid carried, bool attachToGrid = true)
    {
        Drop(carried, attachToGrid);
        CleanupCarrier(carrier, carried);
    }

    private void CleanupCarrier(EntityUid carrier, EntityUid carried)
    {
        RemComp<CarryingComponent>(carrier);
        RemComp<CarryingSlowdownComponent>(carrier);
        _virtualItem.DeleteInHandsMatching(carrier, carried);
        _movementSpeed.RefreshMovementSpeedModifiers(carrier);
    }

    private void Drop(EntityUid carried, bool attachToGrid = true)
    {
        RemComp<BeingCarriedComponent>(carried);
        RemComp<KnockedDownComponent>(carried);
        _actionBlocker.UpdateCanMove(carried);

        if (attachToGrid)
            _transform.AttachToGridOrMap(carried);

        _standingState.Stand(carried);
    }

    private void ApplyCarrySlowdown(EntityUid carrier, EntityUid carried)
    {
        var massRatio = MassContest(carrier, carried);

        if (massRatio == 0f)
            massRatio = 1f;

        var massRatioSquared = Math.Pow(massRatio, 2);
        var modifier = 1 - (0.15 / massRatioSquared);
        modifier = Math.Max(0.1, modifier);

        _slowdown.SetModifier(carrier, (float) modifier);
    }

    public bool CanCarry(EntityUid carrier, Entity<CarriableComponent> carried)
    {
        var handsRequired = carried.Comp.FreeHandsRequired;
        if (HasComp<CarrierOneHandComponent>(carrier))
            handsRequired = 1;

        return carrier != carried.Owner &&
               !HasComp<CarryingComponent>(carrier) &&
               HasComp<MapGridComponent>(Transform(carrier).ParentUid) &&
               !HasComp<BeingCarriedComponent>(carrier) &&
               !HasComp<BeingCarriedComponent>(carried) &&
               TryComp<HandsComponent>(carrier, out var hands) &&
               _hands.CountFreeHands((carrier, hands)) >= handsRequired;
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

    private TimeSpan GetPickupDuration(EntityUid carrier, EntityUid carried)
    {
        var length = TimeSpan.FromSeconds(3);
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
                RemCompDeferred<BeingCarriedComponent>(carried);
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
