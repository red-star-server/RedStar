using Content.Server._RedStar.Emoting.Components;
using Content.Shared._RedStar.Emoting.Components;
using Content.Shared._RedStar.Emoting.Events;
using Content.Shared._RedStar.Emoting.Prototypes;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Emoting;

public sealed partial class PairedEmoteSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private RotateToFaceSystem _rotate = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<EmoteAnimationComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess ||
            !args.CanInteract ||
            ent.Owner == args.User ||
            !CanPerformPairedEmote(ent.Owner) ||
            !CanPerformPairedEmote(args.User))
        {
            return;
        }

        if (HasComp<PairedEmoteOfferComponent>(args.User))
            return;

        var user = args.User;

        foreach (var prototype in ProtoMan.EnumeratePrototypes<PairedEmotePrototype>())
        {
            var pairedEmote = prototype;

            args.Verbs.Add(new InteractionVerb
            {
                Text = Loc.GetString(pairedEmote.Name),
                Icon = pairedEmote.Icon,
                Act = () => TryOffer(user, ent.Owner, pairedEmote),
                Priority = -20
            });
        }
    }

    [SubscribeLocalEvent]
    private void OnInteractHand(Entity<PairedEmoteOfferComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || args.User != ent.Comp.Target)
            return;

        if (ent.Comp.ExpiresAt <= _timing.CurTime ||
            !ProtoMan.TryIndex(ent.Comp.Emote, out var prototype) ||
            !CanPerformPairedEmote(ent.Owner) ||
            !CanPerformPairedEmote(args.User) ||
            !_interaction.InRangeUnobstructed(
                args.User,
                ent.Owner,
                prototype.Range,
                popup: false))
        {
            CancelOffer(ent);
            return;
        }

        args.Handled = true;

        PerformEmote(ent.Owner, args.User, prototype);
        CancelOffer(ent);
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

    private void TryOffer(
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

        if (HasComp<PairedEmoteOfferComponent>(initiator))
            return;

        var offer = EnsureComp<PairedEmoteOfferComponent>(initiator);

        offer.Target = target;
        offer.Emote = prototype.ID;
        offer.ExpiresAt = _timing.CurTime + prototype.OfferDuration;

        _popup.PopupEntity(
            Loc.GetString(
                prototype.AttemptSelf,
                ("target", target)),
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

        var message = Loc.GetString(
            prototype.Success,
            ("initiator", initiator),
            ("target", target));

        _popup.PopupEntity(
            message,
            initiator,
            Filter.Pvs(initiator, entityManager: EntityManager),
            true,
            PopupType.Medium);

        if (prototype.Sound is { } sound)
            _audio.PlayPvs(sound, initiator);

        var filter = Filter.Pvs(initiator, entityManager: EntityManager)
            .Merge(Filter.Pvs(target, entityManager: EntityManager));

        RaiseNetworkEvent(
            new PairedEmoteAnimationEvent(
                GetNetEntity(initiator),
                GetNetEntity(target),
                prototype.InitiatorAnimation,
                prototype.TargetAnimation),
            filter);
    }

    private bool CanPerformPairedEmote(EntityUid uid)
    {
        if (!HasComp<EmoteAnimationComponent>(uid))
            return false;

        return (!TryComp<MobStateComponent>(uid, out var mobState) ||
                mobState.CurrentState == MobState.Alive) &&
               _actionBlocker.CanInteract(uid, null);
    }

    private void CancelOffer(Entity<PairedEmoteOfferComponent> ent)
    {
        RemCompDeferred<PairedEmoteOfferComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<PairedEmoteOfferComponent>();

        while (query.MoveNext(out var uid, out var offer))
        {
            if (time < offer.ExpiresAt)
                continue;

            _popup.PopupEntity(
                Loc.GetString("paired-emote-left-hanging"),
                uid,
                uid,
                PopupType.SmallCaution);

            RemCompDeferred<PairedEmoteOfferComponent>(uid);
        }
    }
}
