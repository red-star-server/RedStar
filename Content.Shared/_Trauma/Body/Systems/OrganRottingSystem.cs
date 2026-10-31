using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;

namespace Content.Shared._Trauma.Body;

/// <summary>
/// Prevents perishable organs/bodyparts from rotting inside a living mob.
/// </summary>
public sealed partial class OrganRottingSystem : EntitySystem
{

    [SubscribeLocalEvent]
    private void OnIsRotting(Entity<OrganComponent> ent, ref IsRottingEvent args)
    {
        args.Handled |= ent.Comp.Body != null;
    }
}
