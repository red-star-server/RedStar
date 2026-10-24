using Content.Shared.Body;
using Content.Shared.Gibbing;
using Robust.Shared.Timing;

namespace Content.Shared._Trauma.Body;

public sealed partial class FragileOrganSystem : EntitySystem
{
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent(after: [typeof(BodyPartSystem)])]
    private void OnRemove(Entity<FragileOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent) || _timing.ApplyingState)
            return;

        _gibbing.Gib(ent.Owner);
    }
}
