using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Popups;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared._Floof.OfferItem;

public abstract partial class SharedOfferItemSystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private void InitializeInteractions()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OfferItem, InputCmdHandler.FromDelegate(SetInOfferMode, handle: false, outsidePrediction: false))
            .Register<SharedOfferItemSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<SharedOfferItemSystem>();
    }

    /// <summary>
    ///     This sets IsInOfferMode to true, allowing the player to select whom to offer an item to with interaction.
    /// </summary>
    private void SetInOfferMode(ICommonSession? offerer)
    {
        if (offerer is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not { Valid: true } uid)
            return;

        if (!Exists(uid))
            return;

        if (!_actionBlocker.CanInteract(uid, null))
            return;

        if (!TryComp<OfferItemComponent>(uid, out var offerItem))
            return;

        if (!TryComp<HandsComponent>(uid, out var hands))
            return;

        if (_hands.GetActiveHand((uid, hands)) is not { } activeHandName)
            return;

        if (offerItem.IsInReceiveMode)
        {
            UnReceive(uid, offerItem);
            return;
        }

        if (offerItem.IsInOfferMode || offerItem.ReceivingFrom != null)
        {
            if (offerItem.ReceivingFrom is { } receiver)
            {
                UnReceive(receiver, offererComp: offerItem);
            }
            else
                UnOffer(uid, offerItem);

            return;
        }

        if (!_hands.TryGetHeldItem((uid, hands), activeHandName, out var heldItem))
        {
            _popup.PopupEntity(Loc.GetString("offer-item-empty-hand"), uid, uid);
            return;
        }

        offerItem.Item = heldItem;
        offerItem.Hand = activeHandName;
        offerItem.IsInOfferMode = true;
        Dirty(uid, offerItem);
    }
}
