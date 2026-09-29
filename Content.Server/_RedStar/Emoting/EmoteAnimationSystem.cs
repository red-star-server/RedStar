using Content.Shared._RedStar.Emoting.Components;
using Content.Shared.Chat;

namespace Content.Server._RedStar.Emoting;

public sealed partial class EmoteAnimationSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnEmote(Entity<EmoteAnimationComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.Animations.TryGetValue(args.Emote.ID, out var animation))
            return;

        ent.Comp.Animation = animation;
        ent.Comp.AnimationSequence++;

        Dirty(ent);
    }
}
