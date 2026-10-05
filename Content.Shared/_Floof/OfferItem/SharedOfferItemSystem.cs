using Content.Shared._DV.Carrying;
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
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private EntityQuery<OfferItemComponent> _offerQuery;
    [Dependency] private EntityQuery<VirtualItemComponent> _virtualItemQuery;

    public override void Initialize()
    {
        base.Initialize();

        InitializeInteractions();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<OfferItemComponent>();
        while (query.MoveNext(out var uid, out var offerItem))
        {
            if (offerItem.ReceivingFrom is { } other &&
                (!_offerQuery.TryComp(other, out var otherComp) || otherComp.ReceivingFrom != uid))
            {
                CancelOffer((uid, offerItem), showPopup: false);
                continue;
            }

            // If the mob no longer holds an item in the original offering hand, clear offering mode
            if (offerItem.Hand != null &&
                (!_hands.TryGetHeldItem(uid, offerItem.Hand, out var held) || held != offerItem.Item))
            {
                CancelOffer((uid, offerItem));
            }
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

        if (!_offerQuery.TryComp(args.User, out var offererComponent))
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
        if (!_offerQuery.TryComp(receiver, out var receiverComponent))
            return;

        var offerer = args.User;
        if (!_offerQuery.TryComp(offerer, out var offererComponent) || offererComponent.Item != virtItem.Owner)
            return;

        // Since this is ranged, we must also check distance, because the interaction system wont check it for us in this case
        if (!Transform(offerer).Coordinates.TryDistance(EntityManager, _transform, Transform(receiver.Value).Coordinates, out var dst)
            || dst > offererComponent.MaxOfferDistance
            || !_interaction.InRangeUnobstructed(offerer, receiver.Value, range: offererComponent.MaxOfferDistance))
            return;

        // VirtualItemSystem also handles this event; a failed offer must not clear its result.
        if (CreateOffer((receiver.Value, receiverComponent), (offerer, offererComponent)))
            args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnMove(Entity<OfferItemComponent> ent, ref MoveEvent args)
    {
        if (_net.IsClient) // Client often mispredicts movement, we cant trust it here
            return;

        if (ent.Comp.ReceivingFrom is not { } other)
            return;

        if (TryComp(other, out TransformComponent? otherTransform) &&
            _transform.InRange(args.NewPosition, otherTransform.Coordinates, ent.Comp.MaxOfferDistance))
            return;

        CancelOffer(ent);
    }

    [SubscribeLocalEvent]
    private void OnCarryTransfer(Entity<BeingCarriedComponent> ent, ref ItemTransferredEvent args)
    {
        if (args.Handled
            || args.PassedItem == args.RealItem // Means the entity is transferred NOT via carrying
            || args.RealItem is not { Valid: true } carried
            || ent.Comp.Carrier is not { Valid: true } oldCarrier
            || oldCarrier != args.User
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
            || HasComp<BeingCarriedComponent>(ent)
            || ent.Comp.Puller != args.User)
            return;

        args.Handled = _pulling.TryStartPull(args.Target, ent, null, ent.Comp);
    }

    #endregion

    #region Offering / Receiving
    /// <summary>
    /// Offers the selected item to an idle recipient.
    /// </summary>
    private bool CreateOffer(Entity<OfferItemComponent> receiver, Entity<OfferItemComponent> offerer)
    {
        var offererComponent = offerer.Comp;
        var receiverComponent = receiver.Comp;
        if (offerer == receiver ||
            receiverComponent.IsInReceiveMode || receiverComponent.IsInOfferMode || receiverComponent.ReceivingFrom != null ||
            offererComponent.IsInReceiveMode || !offererComponent.IsInOfferMode || offererComponent.Item == null)
            return false;

        if (!_hands.TryGetHeldItem(offerer.Owner, offererComponent.Hand, out var held) || held != offererComponent.Item)
            return false;

        receiverComponent.IsInReceiveMode = true;
        receiverComponent.ReceivingFrom = offerer;
        _alertsSystem.ShowAlert(receiver.Owner, receiverComponent.OfferAlert);

        Dirty(receiver, receiverComponent);

        // Keep the target on the offerer so cancellation and distance checks can find the active offer.
        offererComponent.ReceivingFrom = receiver;
        offererComponent.IsInOfferMode = false;

        Dirty(offerer, offererComponent);

        _popup.PopupEntity(
            Loc.GetString("offer-item-try-give",
                ("item", Identity.Entity(GetRealEntity(offererComponent.Item), EntityManager)),
                ("target", Identity.Entity(receiver, EntityManager))),
            offerer,
            offerer);
        // Receiver popup (server side only, not predicted because recipient != local player)
        _popup.PopupEntity(
            Loc.GetString("offer-item-try-give-target",
                ("user", Identity.Entity(receiverComponent.ReceivingFrom.Value, EntityManager)),
                ("item", Identity.Entity(GetRealEntity(offererComponent.Item), EntityManager))),
            offerer,
            receiver,
            Popups.PopupType.Medium);

        return true;
    }


    /// <summary>
    /// Cancels an offer and clears both participants' state.
    /// </summary>
    private void CancelOffer(Entity<OfferItemComponent> ent, bool showPopup = true)
    {
        if (ent.Comp.ReceivingFrom is { } other &&
            _offerQuery.TryComp(other, out var otherComp) && otherComp.ReceivingFrom == ent.Owner)
        {
            var offerer = ent.Comp.IsInReceiveMode ? new Entity<OfferItemComponent>(other, otherComp) : ent;
            var receiver = ent.Comp.IsInReceiveMode ? ent : new Entity<OfferItemComponent>(other, otherComp);
            var realItem = GetRealEntity(offerer.Comp.Item);

            if (showPopup && Exists(realItem))
            {
                _popup.PopupEntity(
                    Loc.GetString("offer-item-no-give",
                        ("item", Identity.Entity(realItem, EntityManager)),
                        ("target", Identity.Entity(receiver, EntityManager))),
                    offerer, offerer);
                _popup.PopupEntity(
                    Loc.GetString("offer-item-no-give-target",
                        ("user", Identity.Entity(offerer, EntityManager)),
                        ("item", Identity.Entity(realItem, EntityManager))),
                    offerer, receiver);
            }

            ClearOffer((other, otherComp));
        }

        ClearOffer(ent);
    }

    private void ClearOffer(Entity<OfferItemComponent> ent)
    {
        ent.Comp.IsInOfferMode = false;
        ent.Comp.IsInReceiveMode = false;
        ent.Comp.Hand = null;
        ent.Comp.Item = null;
        ent.Comp.ReceivingFrom = null;
        _alertsSystem.ClearAlert(ent.Owner, ent.Comp.OfferAlert);
        if (ent.Comp.LifeStage < ComponentLifeStage.Stopping)
            Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnOfferShutdown(Entity<OfferItemComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsServer)
            CancelOffer(ent, showPopup: false);
    }

    private EntityUid GetRealEntity(EntityUid? item)
    {
        return _virtualItemQuery.TryComp(item, out var virtualItem) ? virtualItem.BlockingEntity : item ?? EntityUid.Invalid;
    }

    /// <summary>
    /// Accepting the offer and receive item
    /// </summary>
    public void Receive(Entity<OfferItemComponent?> receiver)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!Resolve(receiver, ref receiver.Comp) || !receiver.Comp.IsInReceiveMode)
            return;

        if (!_offerQuery.TryComp(receiver.Comp.ReceivingFrom, out var offererComponent) ||
            offererComponent.Hand == null ||
            receiver.Comp.ReceivingFrom is not {} sender ||
            !TryComp<HandsComponent>(receiver, out var hands))
            return;

        if (!_hands.TryGetHeldItem(sender, offererComponent.Hand, out var held) ||
            held != offererComponent.Item || offererComponent.ReceivingFrom != receiver.Owner ||
            !_transform.InRange(Transform(sender).Coordinates, Transform(receiver).Coordinates, offererComponent.MaxOfferDistance) ||
            !_interaction.InRangeUnobstructed(sender, receiver.Owner, range: offererComponent.MaxOfferDistance))
        {
            CancelOffer((receiver, receiver.Comp));
            return;
        }

        if (!_actionBlocker.CanInteract(receiver, sender) || !_actionBlocker.CanInteract(sender, receiver))
            return;

        if (offererComponent.Item != null)
        {
            var realItem = GetRealEntity(offererComponent.Item);
            var offeredItem = offererComponent.Item.Value;
            var isVirtualItem = HasComp<VirtualItemComponent>(offeredItem);
            var transferred = TryHandleExtendedTransfer(sender, receiver, offeredItem, realItem);
            if (!transferred &&
                (isVirtualItem || !_hands.TryPickup(receiver, offeredItem, handsComp: hands)))
            {
                _popup.PopupEntity(Loc.GetString(isVirtualItem ? "offer-item-cannot-receive" : "offer-item-full-hand"), receiver, receiver);
                return;
            }

            _popup.PopupEntity(
                Loc.GetString("offer-item-give",
                    ("item", Identity.Entity(realItem, EntityManager)),
                    ("target", Identity.Entity(receiver, EntityManager))),
                sender,
                sender);
            _popup.PopupEntity(
                Loc.GetString("offer-item-give-other",
                    ("user", Identity.Entity(receiver.Comp.ReceivingFrom.Value, EntityManager)),
                    ("item", Identity.Entity(realItem, EntityManager)),
                    ("target", Identity.Entity(receiver, EntityManager))),
                sender,
                Filter.PvsExcept(sender, entityManager: EntityManager),
                true);
        }

        CancelOffer((receiver, receiver.Comp), showPopup: false);
    }
    #endregion
    /// <summary>
    /// Returns true if <see cref="OfferItemComponent.IsInOfferMode"/> = true
    /// </summary>
    protected bool IsInOfferMode(Entity<OfferItemComponent?> ent)
    {
        return _offerQuery.Resolve(ent, ref ent.Comp, false) && ent.Comp.IsInOfferMode;
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
/// Raised on the real entity to attempt transferring it through item offering.
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
