namespace Content.Shared._Trauma.Traumas;

public sealed partial class BonelessSystem : EntitySystem
{
    // imagine if prototypes could remove comps...
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BonelessComponent> ent, ref MapInitEvent args)
    {
        RemComp<BoneComponent>(ent);
    }
}
