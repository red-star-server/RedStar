namespace Content.Shared._Trauma.Traumas;

public sealed partial class BonelessSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BonelessComponent> ent, ref MapInitEvent args)
    {
        RemComp<BoneComponent>(ent);
    }
}
