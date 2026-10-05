using Content.Shared._RedStar.Emoting.Components;
using Content.Shared._RedStar.Emoting.Prototypes;
using Content.Shared.ActionBlocker;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;

namespace Content.Shared._RedStar.Emoting;

/// <summary>
/// Publishes gestures locally on both sides. Offers are executed only by the server implementation.
/// </summary>
public abstract partial class SharedPairedEmoteSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;

    public static readonly VerbCategory Gestures = new("paired-emote-category-gestures", null);

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<EmoteAnimationComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Owner == args.User ||
            !CanPerformPairedEmote(ent.Owner) || !CanPerformPairedEmote(args.User) ||
            HasActiveOffer(args.User))
        {
            return;
        }

        var user = args.User;
        foreach (var prototype in ProtoMan.EnumeratePrototypes<PairedEmotePrototype>())
        {
            args.Verbs.Add(new InteractionVerb
            {
                Text = Loc.GetString(prototype.Name),
                Icon = prototype.Icon,
                Category = Gestures,
                Act = () => TryOffer(user, ent.Owner, prototype),
                Priority = -20
            });
        }
    }

    protected bool CanPerformPairedEmote(EntityUid uid)
    {
        return !Deleted(uid) && HasComp<EmoteAnimationComponent>(uid) &&
               (!TryComp<MobStateComponent>(uid, out var mobState) || mobState.CurrentState == MobState.Alive) &&
               _actionBlocker.CanInteract(uid, null);
    }

    // Client offer state is deliberately not replicated; execution is validated on the server.
    protected virtual bool HasActiveOffer(EntityUid uid) => false;

    protected virtual void TryOffer(EntityUid initiator, EntityUid target, PairedEmotePrototype prototype) { }
}
