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
    /// This sets IsInOfferMode to true, allowing the player to select whom to offer an item to with interaction.
    /// </summary>
    private void SetInOfferMode(ICommonSession? offerer)
    {
        if (offerer?.AttachedEntity is not { Valid: true } uid)
            return;

        if (!Exists(uid))
            return;

        if (!_offerQuery.TryComp(uid, out var offerItem))
            return;

        if (offerItem.IsInOfferMode || offerItem.ReceivingFrom != null)
        {
            CancelOffer((uid, offerItem));
            return;
        }

        if (!_actionBlocker.CanInteract(uid, null))
            return;

        if (!TryComp<HandsComponent>(uid, out var hands))
            return;

        if (_hands.GetActiveHand((uid, hands)) is not { } activeHandName)
            return;

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
