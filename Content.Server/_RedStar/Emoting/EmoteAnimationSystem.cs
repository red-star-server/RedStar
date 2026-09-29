using Content.Shared._RedStar.Emoting.Components;
using Content.Shared._RedStar.Emoting.Events;
using Content.Shared.Chat;
using Robust.Shared.Player;

namespace Content.Server._RedStar.Emoting;

public sealed partial class EmoteAnimationSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnEmote(Entity<EmoteAnimationComponent> ent, ref EmoteEvent args)
    {
        if (args.Emote.Animation is not { } animation)
            return;

        RaiseNetworkEvent(
            new EmoteAnimationEvent(GetNetEntity(ent.Owner), animation),
            Filter.Pvs(ent.Owner, entityManager: EntityManager));
    }
}
