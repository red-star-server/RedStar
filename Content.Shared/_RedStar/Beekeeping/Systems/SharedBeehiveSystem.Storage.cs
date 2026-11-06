using Content.Shared.Storage.Components;
using Content.Shared._RedStar.Beekeeping.Components;

namespace Content.Shared._RedStar.Beekeeping.Systems;

/// <summary>
/// A closing locker takes in a wild hive lying in it. The locker already finds it (the hive has the body of an item),
/// but a locker only accepts items and mobs, and a wild hive is neither: it must not be picked up. So the hive is let
/// through the check, the same way the event allows for anything a system decides belongs inside. Shared, so the
/// client hides the hive the moment the door closes instead of waiting for the server.
/// </summary>
public abstract partial class SharedBeehiveSystem
{
    [SubscribeLocalEvent]
    private void OnStorageBeforeClose(Entity<EntityStorageComponent> ent, ref StorageBeforeCloseEvent args)
    {
        foreach (var entity in args.Contents)
        {
            if (TryComp<BeehiveComponent>(entity, out var hive) && hive.Wild)
                args.BypassChecks.Add(entity);
        }
    }
}
