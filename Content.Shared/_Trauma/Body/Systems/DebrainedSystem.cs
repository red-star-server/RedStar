using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.DelayedDeath;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Speech;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Trauma.Body;

/// <summary>
/// This system handles behavior on entities when they lose their head or their brains are removed.
/// Mind ownership is handled by BrainSystem.
/// </summary>
public sealed partial class DebrainedSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    public static readonly ProtoId<OrganCategoryPrototype> Heart = "Heart";

    [SubscribeLocalEvent]
    private void OnComponentInit(EntityUid uid, DebrainedComponent _, ComponentInit args)
    {
        // Component state replication supplies these changes on the client.
        if (_timing.ApplyingState)
            return;

        EnsureComp<DelayedDeathComponent>(uid);
        EnsureComp<StunnedComponent>(uid);
        _standing.Down(uid);
    }

    [SubscribeLocalEvent]
    private void OnComponentRemove(EntityUid uid, DebrainedComponent _, ComponentRemove args)
    {
        if (TerminatingOrDeleted(uid) || _timing.ApplyingState)
            return;

        RemComp<StunnedComponent>(uid);
        if (_body.GetOrgan(uid, Heart) != null)
            RemComp<DelayedDeathComponent>(uid);
    }

    [SubscribeLocalEvent]
    private void OnSpeakAttempt(EntityUid uid, DebrainedComponent _, SpeakAttemptEvent args)
    {
        _popup.PopupEntity(Loc.GetString("speech-muted"), uid, uid);
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnStandAttempt(EntityUid uid, DebrainedComponent _, StandAttemptEvent args)
    {
        args.Cancel();
    }

    [SubscribeLocalEvent(before: new []{ typeof(DamageableSystem), typeof(BodyRestoreSystem) })]
    private void OnRejuvenate(Entity<DebrainedComponent> ent, ref RejuvenateEvent args)
    {
        RemCompDeferred(ent, ent.Comp);
    }
}
