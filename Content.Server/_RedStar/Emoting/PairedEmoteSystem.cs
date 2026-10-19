using Content.Server._RedStar.Emoting.Components;
using Content.Server.Chat.Systems;
using Content.Shared._DV.Carrying;
using Content.Shared._RedStar.Emoting;
using Content.Shared._RedStar.Emoting.Events;
using Content.Shared._RedStar.Emoting.Prototypes;
using Content.Shared.Alert;
using Content.Shared.Chat;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Emoting;

public sealed partial class PairedEmoteSystem : SharedPairedEmoteSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private RotateToFaceSystem _rotate = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnAcceptAlert(Entity<PairedEmoteReceivingComponent> ent, ref AcceptPairedEmoteAlertEvent args)
    {
        if (args.Handled || args.User != ent.Owner ||
            !TryComp<PairedEmoteOfferComponent>(ent.Comp.Initiator, out var offer) ||
            offer.Target != ent.Owner || args.AlertId != ent.Comp.Alert)
            return;

        var initiator = ent.Comp.Initiator;
        if (offer.ExpiresAt <= _timing.CurTime ||
            !ProtoMan.TryIndex(offer.Emote, out var prototype) ||
            !CanPerformPairedEmote(initiator) ||
            !CanPerformPairedEmote(ent.Owner) ||
            !_interaction.InRangeUnobstructed(
                ent.Owner,
                initiator,
                prototype.Range,
                popup: false))
        {
            CancelOffer((initiator, offer));
            return;
        }

        args.Handled = true;
        CancelOffer((initiator, offer));
        PerformEmote(initiator, ent.Owner, prototype);
    }

    [SubscribeLocalEvent]
    private void OnMove(Entity<PairedEmoteOfferComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        CancelOffer(ent);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<PairedEmoteOfferComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        CancelOffer(ent);
    }

    [SubscribeLocalEvent]
    private void OnRecipientMobStateChanged(Entity<PairedEmoteReceivingComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        if (TryComp<PairedEmoteOfferComponent>(ent.Comp.Initiator, out var offer))
            CancelOffer((ent.Comp.Initiator, offer));
    }

    [SubscribeLocalEvent]
    private void OnStartedCarrying(Entity<CarryingComponent> ent, ref ComponentStartup args)
    {
        CancelOffersInvolving(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnBecameCarried(Entity<BeingCarriedComponent> ent, ref ComponentStartup args)
    {
        CancelOffersInvolving(ent.Owner);
    }

    private void CancelOffersInvolving(EntityUid uid)
    {
        if (TryComp<PairedEmoteOfferComponent>(uid, out var offer))
            CancelOffer((uid, offer));

        if (TryComp<PairedEmoteReceivingComponent>(uid, out var receiving) &&
            TryComp(receiving.Initiator, out offer))
            CancelOffer((receiving.Initiator, offer));
    }

    [SubscribeLocalEvent]
    private void OnOfferShutdown(Entity<PairedEmoteOfferComponent> ent, ref ComponentShutdown args)
    {
        ClearReceiving(ent.Owner, ent.Comp.Target);
    }

    protected override void TryOffer(
        EntityUid initiator,
        EntityUid target,
        PairedEmotePrototype prototype)
    {
        if (!CanPerformPairedEmote(initiator) ||
            !CanPerformPairedEmote(target) ||
            initiator == target)
        {
            return;
        }

        if (!_interaction.InRangeUnobstructed(
                initiator,
                target,
                prototype.Range,
                popup: false))
        {
            return;
        }

        if (HasActiveOffer(initiator) || HasActiveOffer(target))
            return;

        var offer = EnsureComp<PairedEmoteOfferComponent>(initiator);

        offer.Target = target;
        offer.Emote = prototype.ID;
        offer.ExpiresAt = _timing.CurTime + prototype.OfferDuration;

        var receiving = EnsureComp<PairedEmoteReceivingComponent>(target);
        receiving.Initiator = initiator;
        receiving.Alert = prototype.Alert;
        _alerts.ShowAlert(target, prototype.Alert);

        _popup.PopupEntity(
            Loc.GetString(prototype.AttemptSelf, ("target", target)),
            initiator,
            initiator,
            PopupType.Medium);

        _popup.PopupEntity(
            Loc.GetString(
                prototype.AttemptTarget,
                ("initiator", initiator)),
            target,
            target,
            PopupType.Medium);
    }

    private void PerformEmote(
        EntityUid initiator,
        EntityUid target,
        PairedEmotePrototype prototype)
    {
        var initiatorPosition = _transform.GetMapCoordinates(initiator).Position;
        var targetPosition = _transform.GetMapCoordinates(target).Position;

        _rotate.TryFaceCoordinates(initiator, targetPosition);
        _rotate.TryFaceCoordinates(target, initiatorPosition);

        var filter = Filter.Pvs(initiator, entityManager: EntityManager)
            .Merge(Filter.Pvs(target, entityManager: EntityManager));

        _chat.TrySendInGameICMessage(
            initiator,
            Loc.GetString(prototype.Success, ("target", target)),
            InGameICChatType.Emote,
            ChatTransmitRange.Normal,
            checkRadioPrefix: false,
            ignoreActionBlocker: true);

        if (prototype.Sound is { } sound)
            _audio.PlayPvs(sound, initiator);

        RaiseNetworkEvent(
            new PairedEmoteAnimationEvent(
                GetNetEntity(initiator),
                GetNetEntity(target),
                prototype.InitiatorAnimation,
                prototype.TargetAnimation,
                prototype.ApproachOffset),
            filter);
    }

    protected override bool HasActiveOffer(EntityUid uid) =>
        HasComp<PairedEmoteOfferComponent>(uid) || HasComp<PairedEmoteReceivingComponent>(uid);

    private void CancelOffer(Entity<PairedEmoteOfferComponent> ent)
    {
        ClearReceiving(ent.Owner, ent.Comp.Target);
        RemCompDeferred<PairedEmoteOfferComponent>(ent);
    }

    private void ClearReceiving(EntityUid initiator, EntityUid target)
    {
        if (!TryComp<PairedEmoteReceivingComponent>(target, out var receiving) ||
            receiving.Initiator != initiator)
            return;

        _alerts.ClearAlert(target, receiving.Alert);
        RemCompDeferred<PairedEmoteReceivingComponent>(target);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<PairedEmoteOfferComponent>();

        while (query.MoveNext(out var uid, out var offer))
        {
            if (!TryComp<PairedEmoteReceivingComponent>(offer.Target, out var receiving) ||
                receiving.Initiator != uid)
            {
                CancelOffer((uid, offer));
                continue;
            }

            if (time < offer.ExpiresAt)
                continue;

            _popup.PopupEntity(
                Loc.GetString("paired-emote-left-hanging"),
                uid,
                uid,
                PopupType.SmallCaution);

            CancelOffer((uid, offer));
        }
    }
}
