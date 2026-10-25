using Content.Shared._Trauma.ItemSwitch;

namespace Content.Server._Trauma.ItemSwitch;

public sealed class ItemSwitchSystem : SharedItemSwitchSystem
{
    // TODO SHITMED: make this use battery events not this fucking slop
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ItemSwitchComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            CheckPowerAndSwitchState((uid, comp));
        }
    }
}
