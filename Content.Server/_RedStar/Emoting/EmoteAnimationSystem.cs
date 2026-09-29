using Content.Shared._RedStar.Emoting.Components;
using Content.Shared.Chat;

namespace Content.Server._RedStar.Emoting;

public sealed partial class EmoteAnimationSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnEmote(Entity<EmoteAnimationComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled || args.Emote.Animation == null)
            return;

        ent.Comp.Animation = args.Emote.Animation;
        ent.Comp.AnimationSequence++;

        Dirty(ent);
    }
}
