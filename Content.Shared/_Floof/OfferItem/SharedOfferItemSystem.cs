using Content.Shared._DV;
using Content.Shared.Alert;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._Floof.OfferItem;

public abstract partial class SharedOfferItemSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private AlertsSystem _alertsSystem = default!;
    [Dependency] private CarryingSystem _carrying = default!;
    [Dependency] private PullingSystem _pulling = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeInteractions();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<OfferItemComponent, HandsComponent>();
        while (query.MoveNext(out var uid, out var offerItem, out var hands))
        {
            // If the mob no longer holds an item in the original offering hand, clear offering mode
            if (offerItem.Hand != null &&
                (!_hands.TryGetHeldItem((uid, hands), offerItem.Hand, out var held) || held != offerItem.Item))
            {
                if (offerItem.ReceivingFrom != null)
                {
                    UnReceive(offerItem.ReceivingFrom.Value, offererComp: offerItem);
                    offerItem.IsInOfferMode = false;
                    Dirty(uid, offerItem);
                }
                else
                    UnOffer(uid, offerItem);
            }

            if (!offerItem.IsInReceiveMode)
            {
                _alertsSystem.ClearAlert(uid, offerItem.OfferAlert);
                continue;
            }

            _alertsSystem.ShowAlert(uid, offerItem.OfferAlert);
        }
    }

    #region Events
    [SubscribeLocalEvent]
    private void OnAcceptOffer(Entity<OfferItemComponent> ent, ref AcceptOfferAlertEvent args)
    {
        Receive((ent, ent.Comp));
    }

    [SubscribeLocalEvent(before: [typeof(IngestionSystem)])]
    private void OnInteractWithReceiver(Entity<OfferItemComponent> receiver, ref InteractUsingEvent args)
    {
        if (!_timing.IsFirstTimePredicted || _timing.ApplyingState || args.Handled)
            return;

        if (!TryComp<OfferItemComponent>(args.User, out var offererComponent))
            return;

        args.Handled = CreateOffer(receiver, (args.User, offererComponent));
    }

    [SubscribeLocalEvent]
    private void OnRangedInteractWithReceiver(Entity<OfferableVirtualItemComponent> virtItem, ref BeforeRangedInteractEvent args)
    {
        // Virtual items suppress InteractUsingEvent, so handle their ranged interaction here.
        if (!_timing.IsFirstTimePredicted || _timing.ApplyingState)
            return;

        var receiver = args.Target;
        if (!TryComp<OfferItemComponent>(receiver, out var receiverComponent))
            return;

        var offerer = args.User;
        if (!TryComp<OfferItemComponent>(offerer, out var offererComponent) || offererComponent.Item == null)
            return;

        // Since this is ranged, we must also check distance, because the interaction system wont check it for us in this case
        if (!Transform(offerer).Coordinates.TryDistance(EntityManager, _transform, Transform(receiver.Value).Coordinates, out var dst)
            || dst > offererComponent.MaxOfferDistance)
            return;

        args.Handled = CreateOffer((receiver.Value, receiverComponent), (offerer, offererComponent));
    }

    [SubscribeLocalEvent]
    private void OnMove(Entity<OfferItemComponent> ent, ref MoveEvent args)
    {
        if (_net.IsClient) // Client often mispredicts movement, we cant trust it here
            return;

        if (ent.Comp.ReceivingFrom == null)
            return;

        if (_transform.InRange(args.NewPosition, Transform(ent.Comp.ReceivingFrom.Value).Coordinates, ent.Comp.MaxOfferDistance))
            return;

        UnOffer(ent, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnCarryTransfer(Entity<BeingCarriedComponent> ent, ref ItemTransferredEvent args)
    {
        if (args.Handled
            || args.PassedItem == args.RealItem // Means the entity is transferred NOT via carrying
            || args.RealItem is not { Valid: true } carried
            || ent.Comp.Carrier is not { Valid: true } oldCarrier
            || !TryComp<CarriableComponent>(carried, out var carriable))
            return;

        args.Handled = _carrying.TryTransferCarried(oldCarrier, args.Target, (carried, carriable));
    }

    [SubscribeLocalEvent]
    private void OnPulledTransfer(Entity<PullableComponent> ent, ref ItemTransferredEvent args)
    {
        if (args.Handled
            || args.PassedItem == args.RealItem // Means the entity is transferred NOT via pulling
            || args.RealItem is not { Valid: true }
            || ent.Comp.Puller != args.User)
            return;

        args.Handled = _pulling.TryStartPull(args.Target, ent, null, ent.Comp);
    }

    #endregion

    #region Offering / Recieving
    /// <summary>
    /// Attempts to create an offer. Expects offerer.Item to already be set to the offered item, offererComponent.InReceiveMode == true.
    /// Will fail if offerer == receiver or if receiver already has a set TargetOrOfferer, and that person is not the current offerer
    /// </summary>
    private bool CreateOffer(Entity<OfferItemComponent> receiver, Entity<OfferItemComponent> offerer)
    {
        var offererComponent = offerer.Comp;
        var receiverComponent = receiver.Comp;
        if (offerer == receiver ||
            receiverComponent.IsInReceiveMode || receiverComponent.IsInOfferMode || receiverComponent.ReceivingFrom != null ||
            offererComponent.IsInReceiveMode || !offererComponent.IsInOfferMode || offererComponent.Item == null)
            return false;

        receiverComponent.IsInReceiveMode = true;
        receiverComponent.ReceivingFrom = offerer;

        Dirty(receiver, receiverComponent);

        // Keep the target on the offerer so cancellation and distance checks can find the active offer.
        offererComponent.ReceivingFrom = receiver;
        offererComponent.IsInOfferMode = false;

        Dirty(offerer, offererComponent);

        _popup.PopupEntity(
            Loc.GetString("offer-item-try-give",
                ("item", Identity.Entity(offererComponent.GetRealEntity(EntityManager), EntityManager)),
                ("target", Identity.Entity(receiver, EntityManager))),
            offerer,
            offerer);
        // Receiver popup (server side only, not predicted because recipient != local player)
        _popup.PopupEntity(
            Loc.GetString("offer-item-try-give-target",
                ("user", Identity.Entity(receiverComponent.ReceivingFrom.Value, EntityManager)),
                ("item", Identity.Entity(offererComponent.GetRealEntity(EntityManager), EntityManager))),
            offerer,
            receiver,
            Popups.PopupType.Medium);

        return true;
    }



    /// <summary>
    /// Resets the <see cref="OfferItemComponent"/> of the user and the target
    /// </summary>
    protected void UnOffer(EntityUid thisEntity, OfferItemComponent offererComp)
    {
        if (offererComp.ReceivingFrom is { } otherEntity && TryComp<OfferItemComponent>(otherEntity, out var otherOfferer))
        {
            // So this tries to figure out which of these entities do what...
            // if A.OfferItemComponent.Item != null, then A is currently offering an item to A.OfferItemComponent.TargetOrOfferer
            // If it is null, then it is ONLY being offered an item TO.
            if (offererComp.Item != null && _net.IsServer)
            {
                _popup.PopupEntity(
                    Loc.GetString("offer-item-no-give",
                        ("item", Identity.Entity(offererComp.GetRealEntity(EntityManager), EntityManager)), // Floof - resolve virtual items
                        ("target", Identity.Entity(otherEntity, EntityManager))),
                    thisEntity,
                    thisEntity);
                _popup.PopupEntity(
                    Loc.GetString("offer-item-no-give-target",
                        ("user", Identity.Entity(thisEntity, EntityManager)),
                        ("item", Identity.Entity(offererComp.GetRealEntity(EntityManager), EntityManager))),
                    thisEntity,
                    otherEntity);
            }

            else if (otherOfferer.Item != null && _net.IsServer)
            {
                _popup.PopupEntity(
                    Loc.GetString("offer-item-no-give",
                        ("item", Identity.Entity(otherOfferer.GetRealEntity(EntityManager), EntityManager)), // Floof - resolve virtual items
                        ("target", Identity.Entity(thisEntity, EntityManager))),
                    otherEntity,
                    otherEntity);
                _popup.PopupEntity(
                    Loc.GetString("offer-item-no-give-target",
                        ("user", Identity.Entity(otherEntity, EntityManager)),
                        ("item", Identity.Entity(otherOfferer.GetRealEntity(EntityManager), EntityManager))),
                    otherEntity,
                    thisEntity);
            }

            otherOfferer.IsInOfferMode = false;
            otherOfferer.IsInReceiveMode = false;
            otherOfferer.Hand = null;
            otherOfferer.ReceivingFrom = null;
            otherOfferer.Item = null;

            Dirty(otherEntity, otherOfferer);
        }

        offererComp.IsInOfferMode = false;
        offererComp.IsInReceiveMode = false;
        offererComp.Hand = null;
        offererComp.ReceivingFrom = null;
        offererComp.Item = null;

        Dirty(thisEntity, offererComp);
    }


    /// <summary>
    /// Cancels the transfer of the item
    /// </summary>
    protected void UnReceive(EntityUid receiver, OfferItemComponent? receiverComp = null, OfferItemComponent? offererComp = null)
    {
        if (!Resolve(receiver, ref receiverComp)
            || receiverComp.ReceivingFrom is not {} offerer
            || !Resolve(offerer, ref offererComp))
            return;

        // If offererComp.Item != null, then they are actively offering to TargetOrOfferer
        // Normally this method is called right after a transfer is done, but this part can be called from SetInOfferMode when the player presses F again to cancel an ongoing offer
        if (offererComp.Item != null)
        {
            _popup.PopupEntity(
                Loc.GetString("offer-item-no-give",
                    ("item", Identity.Entity(offererComp.GetRealEntity(EntityManager), EntityManager)), // Floof - resolve virtual items
                    ("target", Identity.Entity(receiver, EntityManager))),
                offerer,
                offerer);
            _popup.PopupEntity(
                Loc.GetString("offer-item-no-give-target",
                    ("user", Identity.Entity(receiverComp.ReceivingFrom.Value, EntityManager)), // Floof - resolve virtual items
                    ("item", Identity.Entity(offererComp.GetRealEntity(EntityManager), EntityManager))),
                offerer,
                receiver);
        }

        offererComp.ReceivingFrom = null;
        receiverComp.ReceivingFrom = null;
        offererComp.IsInOfferMode = false;
        offererComp.Item = null;
        offererComp.Hand = null;
        receiverComp.IsInReceiveMode = false;

        Dirty(offerer, offererComp);
        Dirty(receiver, receiverComp);
    }

    /// <summary>
    /// Accepting the offer and receive item
    /// </summary>
    public void Receive(Entity<OfferItemComponent?> receiver)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!Resolve(receiver, ref receiver.Comp))
            return;

        if (!TryComp<OfferItemComponent>(receiver.Comp.ReceivingFrom, out var offererComponent) ||
            offererComponent.Hand == null ||
            receiver.Comp.ReceivingFrom is not {} sender ||
            !TryComp<HandsComponent>(receiver, out var hands))
            return;

        if (!_hands.TryGetHeldItem(sender, offererComponent.Hand, out var held) ||
            held != offererComponent.Item)
        {
            UnReceive(receiver, receiver.Comp, offererComponent);
            return;
        }

        if (offererComponent.Item != null)
        {
            // Floof - check if there's something else handling it first
            var realItem = offererComponent.GetRealEntity(EntityManager);
            var offeredItem = offererComponent.Item.Value;
            var isVirtualItem = HasComp<VirtualItemComponent>(offeredItem);
            var transferred = TryHandleExtendedTransfer(sender, receiver, offeredItem, realItem);
            if (!transferred &&
                (isVirtualItem || !_hands.TryPickup(receiver, offeredItem, handsComp: hands)))
            {
                _popup.PopupEntity(Loc.GetString("offer-item-full-hand"), receiver, receiver);
                return;
            }

            _popup.PopupEntity(
                Loc.GetString("offer-item-give",
                    ("item", Identity.Entity(realItem, EntityManager)), // FLoof - resolve virtual items
                    ("target", Identity.Entity(receiver, EntityManager))),
                sender,
                sender);
            _popup.PopupEntity(
                Loc.GetString("offer-item-give-other",
                    ("user", Identity.Entity(receiver.Comp.ReceivingFrom.Value, EntityManager)),
                    ("item", Identity.Entity(realItem, EntityManager)), // FLoof - resolve virtual items
                    ("target", Identity.Entity(receiver, EntityManager))),
                sender,
                Filter.PvsExcept(sender, entityManager: EntityManager),
                true);
        }

        offererComponent.Item = null;
        UnReceive(receiver, receiver.Comp, offererComponent);
    }
    #endregion
    /// <summary>
    /// Returns true if <see cref="OfferItemComponent.IsInOfferMode"/> = true
    /// </summary>
    protected bool IsInOfferMode(Entity<OfferItemComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp, false) && ent.Comp.IsInOfferMode;
    }

    private bool TryHandleExtendedTransfer(EntityUid user, EntityUid target, EntityUid offeredItem, EntityUid realItem)
    {
        var ev = new ItemTransferredEvent
        {
            User = user,
            Target = target,
            PassedItem = offeredItem,
            RealItem = realItem,
        };
        RaiseLocalEvent(realItem, ref ev);
        return ev.Handled;
    }
}

/// <summary>
/// Raised on the entity that was transferred via item offering.
/// </summary>
[ByRefEvent]
public sealed class ItemTransferredEvent : HandledEntityEventArgs
{
    public EntityUid User;
    public EntityUid Target;

    /// <summary>
    /// The actual item being passed around. Can be a virtual item.
    /// </summary>
    public EntityUid PassedItem;
    /// <summary>
    /// If <see cref="PassedItem"/> is a virtual item, this field contains the real item that was transferred.
    /// </summary>
    public EntityUid? RealItem;
}
