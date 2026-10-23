using Content.Shared.Body;
using Content.Shared.Gibbing;

namespace Content.Shared._Trauma.Body;

public sealed partial class FragileOrganSystem : EntitySystem
{
    [Dependency] private GibbingSystem _gibbing = default!;

    [SubscribeLocalEvent(after: new[] {typeof(BodyPartSystem)})]
    private void OnRemove(Entity<FragileOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _gibbing.Gib(ent.Owner);
    }
}
